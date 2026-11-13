import * as SignalR from '@microsoft/signalr'
import { onScopeDispose } from 'vue'
import { getValidJwt } from '@/services/HttpService'

const HUB_URL = '/hub'
const MAX_RETRY_DELAY_MS = 30_000
const RESYNC_INTERVAL_MS = 5 * 60_000

/** 0s, 1s, 2s, 4s ... capped at 30s. */
export const retryDelay = (retryCount: number) => Math.min(MAX_RETRY_DELAY_MS, 1000 * 2 ** retryCount)

export interface LobbyConnectionOptions {
  joinCode: string
  /** Hub events to listen for, by name. */
  handlers: Record<string, (...args: any[]) => void>
  /**
   * Fetches everything that may have been missed while not listening. Called after every join of the lobby group
   * (first connect, reconnect, resume), every few minutes as a safety net for lost pushes, and when a connection
   * attempt fails, so the page still has data while retries continue.
   */
  resync: () => Promise<void>
  /** Debug output; level 1-3 as in the lobby store's debug messages. */
  log?: (message: string, level: number) => void
}

/**
 * The lobby page's live connection to the server. Pushes (lobby events, messages, game updates) only reach the page
 * while it's in the lobby's group, so every (re)connect rejoins the group and resyncs. The connection is dropped while
 * the page is hidden and re-established when it's shown again. Ends when the calling component unmounts.
 */
export function useLobbyConnection(options: LobbyConnectionOptions) {
  const log = options.log ?? (() => {})

  let connection: SignalR.HubConnection | undefined
  // Bumped whenever the current connection is replaced or paused, so retries scheduled for an older one stop.
  let generation = 0
  let retryTimer: number | undefined
  let resyncTimer: number | undefined
  let isActive = false
  let isResuming = false

  /** Connects and loads the page's data. Resync errors here propagate, so the page can show that loading failed. */
  async function start() {
    isActive = true
    document.addEventListener('visibilitychange', onVisibilityChange)
    window.addEventListener('pageshow', resume)
    window.addEventListener('online', resume)
    await connectAndSync()
  }

  async function stop() {
    isActive = false
    document.removeEventListener('visibilitychange', onVisibilityChange)
    window.removeEventListener('pageshow', resume)
    window.removeEventListener('online', resume)
    await pause()
  }

  async function connectAndSync() {
    const joined = await connect()
    if (!joined) await options.resync()

    window.clearInterval(resyncTimer)
    resyncTimer = window.setInterval(() => safeResync(), RESYNC_INTERVAL_MS)
  }

  /** Replaces any existing connection. Returns whether the first attempt joined the group (and resynced). */
  async function connect(): Promise<boolean> {
    const thisGeneration = ++generation
    window.clearTimeout(retryTimer)
    if (connection && connection.state !== SignalR.HubConnectionState.Disconnected) await connection.stop()

    const next = new SignalR.HubConnectionBuilder()
      // Negotiates WebSockets first, falling back to SSE then long polling. The hub requires auth; SignalR calls this
      // before every (re)connect, so expired tokens get refreshed.
      .withUrl(HUB_URL, { accessTokenFactory: getValidJwt })
      .configureLogging({ log: (_: SignalR.LogLevel, message: string) => log(message, 1) })
      // The default policy gives up after 4 attempts; keep retrying with backoff.
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (ctx) => (ctx.previousRetryCount === 0 ? 0 : retryDelay(ctx.previousRetryCount - 1)) })
      .build()
    connection = next

    for (const [event, handler] of Object.entries(options.handlers)) next.on(event, handler)
    next.onreconnecting(() => log(`Hub connection reconnecting... (State: ${next.state})`, 2))
    // A reconnect gets a new connection ID, so the server no longer has us in the lobby group.
    next.onreconnected(async () => {
      log(`Hub connection reconnected. (State: ${next.state})`, 2)
      try {
        await joinAndSync(next)
      } catch (err) {
        report(err)
      }
    })

    return attempt(next, thisGeneration, 1)
  }

  // Automatic reconnect only covers drops after a successful start, so retry the initial start here
  // (e.g. on resume the network often isn't ready for a second or two).
  async function attempt(next: SignalR.HubConnection, thisGeneration: number, attemptNumber: number): Promise<boolean> {
    if (thisGeneration !== generation) return false

    try {
      if (next.state === SignalR.HubConnectionState.Disconnected) {
        await next.start()
        log(`Hub connection started. (State: ${next.state})`, 2)
      }
      await joinAndSync(next)
      return true
    } catch (err) {
      report(err)
      if (thisGeneration !== generation || document.hidden) return false
      // Automatic reconnect is already handling it and will rejoin in onreconnected.
      if (next.state === SignalR.HubConnectionState.Reconnecting) return false

      const delay = retryDelay(attemptNumber - 1)
      log(`Hub connection failed (attempt #${attemptNumber}). Retrying in ${delay / 1000} seconds.`, 2)
      retryTimer = window.setTimeout(() => attempt(next, thisGeneration, attemptNumber + 1), delay)
      return false
    }
  }

  async function joinAndSync(next: SignalR.HubConnection) {
    await next.invoke('JoinLobby', options.joinCode)
    log(`Hub connection "Join Lobby" invoked. (State: ${next.state})`, 2)
    await options.resync()
  }

  async function pause() {
    log('Pausing lobby connection.', 2)
    generation++
    window.clearTimeout(retryTimer)
    window.clearInterval(resyncTimer)
    await connection?.stop()
  }

  function onVisibilityChange() {
    if (document.hidden) pause().catch(report)
    else resume()
  }

  // visibilitychange, pageshow and online can all fire when the page comes back; only run one resume at a time.
  async function resume() {
    if (!isActive || document.hidden || isResuming) return
    if (connection?.state === SignalR.HubConnectionState.Connected) return

    isResuming = true
    try {
      await connectAndSync()
    } catch (err) {
      report(err)
    } finally {
      isResuming = false
    }
  }

  async function safeResync() {
    try {
      await options.resync()
    } catch (err) {
      report(err)
    }
  }

  function report(err: unknown) {
    console.error(err)
    log(String(err), 3)
  }

  /** Connection details for the chat's /connection command. */
  function describe() {
    return { id: connection?.connectionId, baseUrl: connection?.baseUrl, state: connection?.state }
  }

  onScopeDispose(() => {
    stop().catch(report)
  })

  return { start, stop, describe }
}
