import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'

// A fake HubConnection per build, so tests can drive start failures, reconnects and incoming events.
const { connections, HubConnectionState, network } = vi.hoisted(() => {
  const HubConnectionState = { Disconnected: 'Disconnected', Connecting: 'Connecting', Connected: 'Connected', Reconnecting: 'Reconnecting' }
  // How many upcoming start() calls fail, across connections.
  return { connections: [] as FakeConnection[], HubConnectionState, network: { failingStarts: 0 } }
})

interface FakeConnection {
  state: string
  start: ReturnType<typeof vi.fn>
  stop: ReturnType<typeof vi.fn>
  invoke: ReturnType<typeof vi.fn>
  handlers: Record<string, (...args: any[]) => void>
  reconnected?: () => Promise<void>
  on(event: string, handler: (...args: any[]) => void): void
  onreconnecting(cb: () => void): void
  onreconnected(cb: () => Promise<void>): void
}

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    withUrl() { return this }
    configureLogging() { return this }
    withAutomaticReconnect() { return this }
    build(): FakeConnection {
      const connection: FakeConnection = {
        state: HubConnectionState.Disconnected,
        start: vi.fn(async () => {
          if (network.failingStarts > 0) {
            network.failingStarts--
            throw new Error('network')
          }
          connection.state = HubConnectionState.Connected
        }),
        stop: vi.fn(async () => { connection.state = HubConnectionState.Disconnected }),
        invoke: vi.fn(async () => {}),
        handlers: {},
        on(event, handler) { connection.handlers[event] = handler },
        onreconnecting() {},
        onreconnected(cb) { connection.reconnected = cb }
      }
      connections.push(connection)
      return connection
    }
  }
  return { HubConnectionBuilder, HubConnectionState }
})
vi.mock('@/services/HttpService', () => ({ getValidJwt: vi.fn() }))

import { useLobbyConnection } from '@/composables/useLobbyConnection'

let hidden = false
Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden })

function setVisible(visible: boolean) {
  hidden = !visible
  document.dispatchEvent(new Event('visibilitychange'))
}

const flush = () => vi.advanceTimersByTimeAsync(0)

describe('useLobbyConnection', () => {
  let scope: ReturnType<typeof effectScope>
  let resync: ReturnType<typeof vi.fn>
  let onGameUpdated: ReturnType<typeof vi.fn>

  function create() {
    scope = effectScope()
    return scope.run(() =>
      useLobbyConnection({ joinCode: 'ABCD', handlers: { GameUpdated: onGameUpdated }, resync })
    )!
  }

  beforeEach(() => {
    vi.useFakeTimers()
    vi.spyOn(console, 'error').mockImplementation(() => {})
    connections.length = 0
    network.failingStarts = 0
    hidden = false
    resync = vi.fn(async () => {})
    onGameUpdated = vi.fn()
  })

  afterEach(() => {
    scope?.stop()
    vi.useRealTimers()
  })

  it('connects, joins the lobby group, then resyncs once', async () => {
    await create().start()

    const [connection] = connections
    expect(connection.start).toHaveBeenCalledOnce()
    expect(connection.invoke).toHaveBeenCalledWith('JoinLobby', 'ABCD')
    expect(resync).toHaveBeenCalledOnce()
    expect(connection.invoke.mock.invocationCallOrder[0]).toBeLessThan(resync.mock.invocationCallOrder[0])
  })

  it('passes hub events to the handlers', async () => {
    await create().start()

    connections[0].handlers.GameUpdated({ id: 1 })

    expect(onGameUpdated).toHaveBeenCalledWith({ id: 1 })
  })

  it('still loads data when the first attempt fails, then keeps retrying with backoff', async () => {
    network.failingStarts = 2

    await create().start()

    expect(resync).toHaveBeenCalledOnce() // loaded without a connection
    await vi.advanceTimersByTimeAsync(1000) // retry 1 fails
    await vi.advanceTimersByTimeAsync(2000) // retry 2 succeeds

    expect(connections[0].invoke).toHaveBeenCalledWith('JoinLobby', 'ABCD')
    expect(resync).toHaveBeenCalledTimes(2)
  })

  it('rejoins the group and resyncs after an automatic reconnect', async () => {
    await create().start()

    await connections[0].reconnected!()

    expect(connections[0].invoke).toHaveBeenCalledTimes(2)
    expect(resync).toHaveBeenCalledTimes(2)
  })

  it('disconnects while hidden and reconnects, rejoins and resyncs when shown', async () => {
    await create().start()

    setVisible(false)
    await flush()
    expect(connections[0].stop).toHaveBeenCalled()

    setVisible(true)
    await flush()
    expect(connections).toHaveLength(2)
    expect(connections[1].invoke).toHaveBeenCalledWith('JoinLobby', 'ABCD')
    expect(resync).toHaveBeenCalledTimes(2)
  })

  it('does nothing on resume events while still connected', async () => {
    await create().start()

    window.dispatchEvent(new Event('online'))
    window.dispatchEvent(new Event('pageshow'))
    await flush()

    expect(connections).toHaveLength(1)
    expect(resync).toHaveBeenCalledOnce()
  })

  it('cancels pending retries when the page is hidden', async () => {
    network.failingStarts = 100
    await create().start()

    setVisible(false)
    await flush()
    await vi.advanceTimersByTimeAsync(60_000)

    expect(connections[0].start).toHaveBeenCalledOnce()
  })

  it('resyncs every five minutes as a safety net for lost pushes', async () => {
    await create().start()

    await vi.advanceTimersByTimeAsync(5 * 60_000)
    await vi.advanceTimersByTimeAsync(5 * 60_000)

    expect(resync).toHaveBeenCalledTimes(3)
  })

  it('stops everything when its component goes away', async () => {
    await create().start()

    scope.stop()
    await flush()
    setVisible(false)
    setVisible(true)
    await vi.advanceTimersByTimeAsync(10 * 60_000)

    expect(connections[0].stop).toHaveBeenCalled()
    expect(connections).toHaveLength(1)
    expect(resync).toHaveBeenCalledOnce()
  })
})
