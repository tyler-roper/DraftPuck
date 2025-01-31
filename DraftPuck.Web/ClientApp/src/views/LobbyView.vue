<script setup lang="ts">
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { computed, nextTick, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import * as SignalR from '@microsoft/signalr'
import { compareAsc, addHours, isWithinInterval } from 'date-fns'
import GameService from '@/services/GameService'
import LobbyEventType from '@/enums/lobbyEventType'
import { parseAllDates } from '@/helpers/dateHelpers'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'
import FeedItem from '@/models/feedItem'
import LoadingMessages from '@/models/loadingMessages'
import VInstructionsModal from '@/components/VInstructionsModal.vue'
import VGameScoreboards from '@/components/VGameScoreboards.vue'
import VLobbyOverview from '@/components/VLobbyOverview.vue'
import VFeed from '@/components/VFeed.vue'
import MessageViewModel from '@/models/messageViewModel'
import VChat from '@/components/VChat.vue'
import LobbyService from '@/services/LobbyService'
import GameState from '@/enums/gameState'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'
import { initializeApp } from 'firebase/app'
import { getToken, getMessaging, onMessage } from 'firebase/messaging'
import UserService from '@/services/UserService'

//const
type View = 'feed' | 'game' | 'lobby' | 'chat'
const LOCAL_STORAGE_KEY = 'latestLobby'
const ACTIVE_GAME_POLLING_INTERVAL_MS = 10000
const INACTIVE_GAME_POLLING_INTERVAL_MS = 60000
const DRINK_ANIMATION_DURATION_MS = 5000
const HUB_URL = '/hub'

const replaceTemplatedStrings = (lobbyEvent: LobbyEvent) => parseLobbyEventText(lobbyEvent, lobby.value!, games.value)

const lobbyEventHandlers: { [k: string]: (lobbyEvent: LobbyEvent) => void } = {
  onDrinkAssigned: function (lobbyEvent: LobbyEvent) {
    if (lobbyEvent.lobbyMember2Id === currentLobbyMember.value?.id) notifyCurrentUserOfDrink(lobbyEvent)
  },
  onDrinkAwarded: function (lobbyEvent: LobbyEvent) {
    if (lobbyEvent.lobbyMemberId === currentLobbyMember.value?.id) notifyCurrentUserOfCorrectPick(lobbyEvent)
  }
}

const commands = computed<{ [command: string]: (...args: string[]) => void }>(() => ({
  debug: (level?: string) => {
    const newLevel = level == undefined || isNaN(+level) ? 1 : +level
    const newLevelClamped = Math.min(3, newLevel)
    setDebugging(newLevelClamped)
    if (newLevelClamped === 0) sendSystemMessage(`Debugging disabled.`)
    else sendSystemMessage(`Debugging enabled (Level ${newLevelClamped}).`)
  },
  connection: () => {
    sendSystemMessage(
      JSON.stringify(
        {
          id: hubConnection.connectionId,
          baseUrl: hubConnection.baseUrl,
          state: hubConnection.state
        },
        undefined,
        2
      )
    )
  },
  me: () => {
    sendSystemMessage(JSON.stringify(getLobbyMemberInfo(), undefined, 2))
  },
  user: (...nameParts: string[]) => {
    const name = nameParts.join(' ')
    const lobbyMember = lobby.value?.members.find((m) => m.name.toUpperCase() === name.toUpperCase())
    if (lobbyMember) {
      sendSystemMessage(JSON.stringify(getLobbyMemberInfo(name), undefined, 2))
    } else {
      sendSystemMessage(`User ${name} not found.`)
    }
  }
}))

//data
const store = useLobbyStore()
const route = useRoute()
const router = useRouter()
const toast = useToast()
const { lobby, currentUserId, events, systemMessages } = storeToRefs(store)
const { getLobby, getLobbyEvents, addLobbyEvent, addMessageToStore, sendDebugMessage, sendSystemMessage, setDebugging } = store
const joinCode = ref(route.params.joinCode as string)
const games = ref<Game[]>([])
const isInstructionsVisible = ref(false)
const isInvalidLobby = ref(false)
const isLoading = ref(false)
const mappedEvents = ref<LobbyEvent[]>([])
const timers = ref<Number[]>([])
const currentView = ref<View>('lobby')
const pendingDrinks = ref<LobbyEvent[]>([])
const currentDrink = ref<LobbyEvent>()
const unseenMessageCount = ref(0)
const vChat = ref<InstanceType<typeof VChat> | null>(null)
const shouldAnimateFeed = ref(false)
const feedAnimationTimer = ref<number>()
const isInitialLoad = ref(true)
const notificationPermissionsGranted = ref(Notification.permission === 'granted')
const notificationsSupported = ref('Notification' in window)

let hubConnection: SignalR.HubConnection

//computed
const feedItems = computed(getFeedItems)
const currentLobbyMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const loadingMessage = computed(() => LoadingMessages.random())
const isLobbyView = computed(() => currentView.value === 'lobby')
const isFeedView = computed(() => currentView.value === 'feed')
const isGameView = computed(() => currentView.value === 'game')
const isChatView = computed(() => currentView.value === 'chat')

const is4Nations = computed(() => {
  const today = new Date()
  const firstDay = new Date(2025, 1, 12)
  const lastDay = new Date(2025, 1, 20)
  const paddingHours = 12
  const start = addHours(firstDay, -1 * paddingHours)
  const end = addHours(lastDay, paddingHours)

  return isWithinInterval(today, { start, end })
})

const pendingDrinkCount = computed(
  () => currentLobbyMember.value?.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)).length ?? 0
)
const messages = computed(() => {
  if (!lobby.value) return []

  const lobbyMemberMessages = lobby.value.members.reduce(
    (messages: MessageViewModel[], member) => [
      ...messages,
      ...(member.messages?.map((msg) => new MessageViewModel(member, msg.message, msg.sent, msg.id)) ?? [])
    ],
    []
  )

  const allMessages = [...lobbyMemberMessages, ...systemMessages.value]
  return allMessages.sort((a, b) => compareAsc(a.sent, b.sent))
})

//hooks/methods
;(async function onCreated() {
  try {
    isLoading.value = true
    await getLobby(joinCode.value)
    if (!lobby.value) throw 'Lobby not found'

    if (!currentLobbyMember.value) {
      let name: string | null = ''
      name = prompt('Welcome! Choose a name...')

      while (name !== null && lobby.value?.members.some((m) => m.name.toLowerCase() === name?.toLowerCase()))
        name = prompt('Sorry, that name is taken. Try another...')

      if (!name) return router.push({ name: 'Home' })

      await LobbyService.joinLobbyByCode(joinCode.value, name)
      await getLobby(joinCode.value)
    }

    if (!currentLobbyMember.value) return router.push({ name: 'Home' })
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({ joinCode: joinCode.value, name: currentLobbyMember.value.name }))

    await getLobbyEvents(lobby.value.id)
    await initializeHubConnection()
    await initializeFirebase()
    await setGames()

    setTimeout(() => {}, 100)

    mappedEvents.value = events.value.map(replaceTemplatedStrings)
  } catch (e) {
    logError(e as string)
    isInvalidLobby.value = true
  } finally {
    isLoading.value = false
  }
})()

const isGameInProgress = (game: Game) => game.gameState === GameState.Live
const isGameOver = (game: Game) => game.gameState === GameState.Final
const isGameStale = (game: Game) => isGameOver(game)

function setView(view: View) {
  currentView.value = view
}

async function updateUserFcmToken(token?: string) {
  await UserService.updateFcmRegistrationToken(currentUserId.value!, { token })
}

async function requestNotificationPermission() {
  await Notification.requestPermission()
  notificationPermissionsGranted.value = Notification.permission === 'granted'
  initializeFirebase()
}

async function initializeFirebase() {
  if (!notificationsSupported || !notificationPermissionsGranted.value) {
    await updateUserFcmToken()
    return
  }

  const vapidKey = import.meta.env.VITE_FIREBASE_VAPID_KEY
  const firebaseConfig = {
    apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
    authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
    projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
    storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
    messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
    appId: import.meta.env.VITE_FIREBASE_APP_ID
  }

  try {
    const app = initializeApp(firebaseConfig)
    const messaging = getMessaging(app)
    onMessage(messaging, (payload) => console.log('Message received:', payload))
    const token = await getToken(messaging, { vapidKey })
    await updateUserFcmToken(token)
  } catch (e) {
    console.error(`Unable to initialize firebase`, e)
    sendSystemMessage(`Unable to initialize firebase ${e}`)
  }
}

function logError(error: string) {
  console.error(error)
  sendDebugMessage(error, 3)
}

async function setGames() {
  sendDebugMessage(`Setting games...`, 1)
  games.value = await GameService.getAllGames()
  sendDebugMessage(`${games.value.length} games retrieved.`, 1)
  games.value.forEach((game) => {
    if (!isGameStale(game)) timers.value.push(window.setTimeout(() => pollForUpdates(game), ACTIVE_GAME_POLLING_INTERVAL_MS))
  })
}

async function updateGame(gameId: number) {
  const gameIndex = games.value.findIndex((g) => gameId === g.id)
  games.value[gameIndex] = await getGameData(gameId)
}

async function getGameData(gameId: number) {
  return await GameService.getGame(gameId)
}

async function pollForUpdates(game: Game) {
  const msgPrefix = `[${game.awayTeam.abbreviation} @ ${game.homeTeam.abbreviation}]`
  try {
    await updateGame(game.id)
    if (isGameStale(game)) {
      sendDebugMessage(`${msgPrefix} Game is stale. `, 1)
      return
    }

    const interval = isGameInProgress(game) ? ACTIVE_GAME_POLLING_INTERVAL_MS : INACTIVE_GAME_POLLING_INTERVAL_MS

    sendDebugMessage(`${msgPrefix} Updated. (Next update in ${interval / 1000} seconds)`, 1)
    timers.value.push(window.setTimeout(() => pollForUpdates(game), interval))
  } catch (e) {
    logError(`${msgPrefix} ${e as string}`)
  }
}

async function initializeHubConnection() {
  hubConnection = new SignalR.HubConnectionBuilder()
    .withUrl(HUB_URL, SignalR.HttpTransportType.ServerSentEvents)
    .configureLogging(SignalR.LogLevel.Error)
    .withAutomaticReconnect()
    .build()

  hubConnection.on('LobbyEvent', dispatchLobbyEvent)
  hubConnection.on('Message', onNewMessage)
  hubConnection.onreconnecting(() => sendDebugMessage(`Hub connection reconnecting... (State: ${hubConnection.state})`, 2))
  hubConnection.onreconnected(() => sendDebugMessage(`Hub connection reconnected. (State: ${hubConnection.state})`, 2))

  try {
    await hubConnection.start()
    sendDebugMessage(`Hub connection started. (State: ${hubConnection.state})`, 2)
    await hubConnection.invoke('JoinLobby', joinCode.value)
    sendDebugMessage(`Hub connection "Join Lobby" invoked. (State: ${hubConnection.state})`, 2)
  } catch (err) {
    logError(err as string)
  }
}

function onNewMessage(message: Message) {
  if (message.lobbyMemberId === currentLobbyMember.value?.id) return
  parseAllDates(message)
  addMessageToStore(message)

  if (currentView.value !== 'chat') unseenMessageCount.value++
}

function notifyCurrentUserOfDrink(lobbyEvent: LobbyEvent) {
  pendingDrinks.value.push(lobbyEvent)

  if (pendingDrinks.value.length === 1) processNextDrinkForCurrentUser()
}

function processNextDrinkForCurrentUser() {
  if (pendingDrinks.value.length === 0) return
  currentDrink.value = pendingDrinks.value[0]

  window.setTimeout(async () => {
    pendingDrinks.value.splice(0, 1)
    currentDrink.value = undefined
    await nextTick()
    processNextDrinkForCurrentUser()
  }, DRINK_ANIMATION_DURATION_MS)
}

function getSenderNameByLobbyEvent(lobbyEvent: LobbyEvent) {
  return lobby.value!.members.find((m) => m.id === lobbyEvent.lobbyMemberId)?.name
}

async function dispatchLobbyEvent(lobbyEvent: LobbyEvent) {
  const currentLobbyMemberValue = currentLobbyMember.value!

  sendDebugMessage(`New ${LobbyEventType[lobbyEvent.lobbyEventType]} Event`, 3)
  sendDebugMessage(JSON.stringify(lobbyEvent, undefined, 4), 2)

  if (lobbyEvent.lobbyEventType != LobbyEventType.NewPick || lobbyEvent.lobbyMemberId !== currentLobbyMemberValue.id) {
    await getLobby(joinCode.value)
    if (!lobby.value) return
  }

  if (lobbyEvent.lobbyEventType === LobbyEventType.UserRemoved && lobbyEvent.lobbyMemberId === currentLobbyMemberValue.id) {
    toast.error('You were removed from the lobby.')
    return router.push({ name: 'Home' })
  }

  parseAllDates(lobbyEvent)
  addLobbyEvent(lobbyEvent)
  mappedEvents.value.push(replaceTemplatedStrings(lobbyEvent))

  const eventType = LobbyEventType[lobbyEvent.lobbyEventType]
  const eventHandler = lobbyEventHandlers[`on${eventType}`]

  if (eventHandler) eventHandler(lobbyEvent)
}

function notifyCurrentUserOfCorrectPick(lobbyEvent: LobbyEvent) {
  const player = games.value.flatMap((g) => Object.values(g.playerSummaries)).find((p) => p.id === lobbyEvent.playerId)

  const playerMsg = player ? ` for a goal by ${player.firstName} ${player.lastName}` : ''
  const msg = `Give out a drink${playerMsg}!`
  toast.success(msg)
}

function getFeedItems() {
  if (!lobby.value) return []
  const desiredPlayTypes = [PlayType.Goal, PlayType.PeriodStart, PlayType.PeriodEnd, PlayType.GameEnd, PlayType.Challenge, PlayType.Penalty]

  const gameItems = games.value.flatMap((game) => {
    return game.plays.reduce((items: FeedItem[], play) => {
      const includedInFilters = desiredPlayTypes.includes(play.type)
      const happenedAfterLobbyStarted = play.dateTime >= lobby.value!.created
      const isShootoutGoal = play.type === PlayType.Goal && play.periodType === PeriodType.Shootout
      if (includedInFilters && happenedAfterLobbyStarted && !isShootoutGoal) {
        return [...items, FeedItem.fromPlay(game.id, { away: game.awayTeam, home: game.homeTeam }, play, game.playerSummaries)]
      } else return items
    }, [])
  })

  const lobbyItems = mappedEvents.value.map((evt) => FeedItem.fromLobbyEvent(evt))
  const feedItems = [...gameItems, ...lobbyItems]
  feedItems.sort((a, b) => compareAsc(a.time, b.time))
  return feedItems
}

async function setViewToChat() {
  setView('chat')
  unseenMessageCount.value = 0

  await nextTick()
  vChat.value?.focus()
}

function animateFeed() {
  if (isInitialLoad.value) {
    window.setTimeout(() => (isInitialLoad.value = false), 2000)
    return
  }

  shouldAnimateFeed.value = true
  window.clearTimeout(feedAnimationTimer.value)
  window.setTimeout(() => (shouldAnimateFeed.value = false), 700)
}

function handleCommand(command: string, ...args: [string]) {
  if (commands.value[command]) commands.value[command](...args)
}

function getLobbyMemberInfo(name?: string): Partial<LobbyMember> | undefined {
  const lobbyMember = name ? lobby.value?.members.find((m) => m.name.toUpperCase() === name.toUpperCase()) : currentLobbyMember.value

  if (!lobbyMember) return
  const { messages, picks, ...lobbyMemberInfo } = lobbyMember
  return lobbyMemberInfo
}

//watch
watch(
  () => feedItems.value.length,
  (newLength, oldLength) => {
    if (newLength > oldLength) {
      animateFeed()
    }
  }
)
</script>

<template>
  <div class="d-flex overflow-hidden flex-column" style="height: 100%">
    <template v-if="!isInvalidLobby">
      <VInstructionsModal v-if="isInstructionsVisible" :join-code="lobby?.joinCode" @close="isInstructionsVisible = false" />

      <div class="bg-stone-900 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center justify-content-between" style="z-index: 10">
        <router-link to="/" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
          <img v-if="!is4Nations" src="/img/logo-wide.png" />
          <img v-if="is4Nations" src="/img/logo-wide-4nations.png" />
        </router-link>

        <a
          role="button"
          v-if="!notificationPermissionsGranted && notificationsSupported"
          class="text-decoration-none text-uppercase fw-bold mt-1 fs-8"
          @click="requestNotificationPermission"
          >Enable Notifications</a
        >
        <span class="fw-bold mt-1 fs-8">Notifications enabled.</span>
        <!-- <a target="_blank" class="text-decoration-none text-uppercase fw-bold mt-1 fs-8" href="https://discord.gg/Vgj9RbetDB">Join the Discord</a> -->

        <a class="d-flex pt-1 text-stone-0 fw-bold text-decoration-none align-items-center" role="button" @click="isInstructionsVisible = true">
          <i class="fi fi-rr-question-square d-block fs-3" style="line-height: 20px"></i>
          <span class="d-none d-sm-block text-uppercase ms-2" style="margin-top: -2px">How To Play</span>
        </a>
      </div>

      <div class="d-flex flex-grow-1 overflow-hidden bg-stone-800">
        <template v-if="!isLoading">
          <VGameScoreboards class="full-scoreboard flex-grow-1" :class="{ 'hide-mobile': !isGameView }" :games="games" style="overflow: auto" />

          <div
            class="feed flex-shrink-0 d-flex flex-column"
            :class="{ 'hide-mobile': !isFeedView && !isLobbyView && !isChatView }"
            style="width: 400px"
          >
            <VLobbyOverview ref="overview" class="lobby-overview v-lobby-overview flex-grow-1" :class="{ 'hide-mobile': !isLobbyView }" />
            <VFeed class="flex-grow-1 v-feed" :items="feedItems" :class="{ 'hide-mobile': !isFeedView, animate: shouldAnimateFeed }" />
            <VChat
              ref="vChat"
              :messages="messages"
              class="flex-grow-1 v-chat"
              :class="{ 'hide-mobile': !isChatView }"
              @command="handleCommand"
            ></VChat>
          </div>
        </template>

        <div v-if="isLoading" style="width: 100%; height: 100%" class="d-flex align-items-center">
          <div class="mx-auto d-flex flex-column align-items-center">
            <div class="spinner-border text-white" style="width: 150px; height: 150px" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
            <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase fw-bold">{{ loadingMessage }}...</span>
          </div>
        </div>
      </div>

      <div class="bottom-nav d-flex d-sm-none bg-stone-900 shadow fw-bold" v-if="!isLoading">
        <a role="button" class="text-center p-2 text-white" :class="{ active: isLobbyView }" @click="setView('lobby')">
          <i v-if="pendingDrinkCount <= 0" class="fi fi-rr-users-alt"></i>
          <span v-if="pendingDrinkCount > 0" class="drink-badge">🚨 {{ pendingDrinkCount }}</span>
          <br />
          <span>LOBBY</span>
        </a>
        <a role="button" class="text-center p-2 text-white" :class="{ active: isFeedView }" @click="setView('feed')">
          <i class="fi fi-rr-list"></i><br />
          <span>FEED</span>
        </a>
        <a role="button" class="text-center p-2 text-white" :class="{ active: isGameView }" @click="setView('game')">
          <i class="fi fi-rr-hockey-puck"></i><br />
          <span>SCORES</span>
        </a>
        <a role="button" class="text-center p-2 text-white" :class="{ active: isChatView }" @click="setViewToChat">
          <i v-if="unseenMessageCount <= 0" class="fi fi-rr-comment-alt"></i>
          <span v-if="unseenMessageCount > 0" class="drink-badge">💬 {{ unseenMessageCount }}</span>
          <br />
          <span>CHAT</span>
        </a>
      </div>
    </template>

    <template v-if="isInvalidLobby">
      <div style="width: 100%; height: 100%" class="d-flex align-items-center">
        <div class="mx-auto d-flex flex-column align-items-center">
          <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase fw-bold">Sorry, this lobby is invalid.</span>
        </div>
      </div>
    </template>

    <div v-if="currentDrink" class="drink-animation d-flex align-items-center justify-content-center">
      <span class="text-white text-center">
        <span class="text-uppercase fw-bold" style="font-size: 100px">Drink!</span>
        <span class="d-block fs-5" style="opacity: 0.5"> Courtesy Of </span>
        <span class="d-block fw-bold fs-2 text-uppercase">
          {{ getSenderNameByLobbyEvent(currentDrink) }}
        </span>
      </span>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

@keyframes bouncein {
  0% {
    transform: translate(-50%, -50%) scale(0.4);
    opacity: 0;
    animation-timing-function: cubic-bezier(0.34, 1.56, 0.64, 1);
  }

  25% {
    transform: translate(-50%, -50%) scale(1.08);
    opacity: 1;
    animation-timing-function: ease-out;
  }

  100% {
    transform: translate(-50%, -50%) scale(1);
    animation-timing-function: ease-in-out;
  }
}

.drink-animation {
  position: absolute;
  z-index: 99;
  max-width: 100%;
  width: 375px;
  max-height: 100%;
  height: 375px;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background-color: map-get($custom-colors, 'amber-500');
  border-radius: 20px;
  box-shadow: 0 0 25px black;
  animation: bouncein;
  animation-duration: 2.5s;
  animation-iteration-count: 2;
  animation-direction: alternate;
  animation-fill-mode: forwards;
}

.bottom-nav {
  border: 2px solid map-get($custom-colors, 'stone-900');
  box-shadow: 0 0 10px black;
  position: relative;
  z-index: 10;
}

.bottom-nav > a {
  display: block;
  width: calc(100% / 3);
  text-decoration: none !important;
  position: relative;
}

.bottom-nav > a:not(.active):hover {
  background-color: map-get($custom-colors, 'stone-800') !important;
}

.bottom-nav > a.active {
  background-color: map-get($custom-colors, 'stone-300') !important;
  color: map-get($custom-colors, 'stone-900') !important;
}

.bottom-nav > a:not(:first-child) {
  border-left: 1px solid map-get($custom-colors, 'stone-800');
}

.drink-badge {
  display: inline-block;
  background-color: map-get($custom-colors, 'stone-0');
  color: map-get($custom-colors, 'stone-900');
  padding-left: 7px;
  padding-right: 9px;
  border-radius: 20px;
}
</style>
