<script setup lang="ts">
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { computed, nextTick, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import * as SignalR from '@microsoft/signalr'
import { addHours, compareAsc, format } from 'date-fns'
import NHL from '@/services/NhlService'
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

//const
type View = 'feed' | 'game' | 'lobby' | 'chat'
const LOCAL_STORAGE_KEY = 'latestLobby'
const ACTIVE_GAME_POLLING_INTERVAL_MS = 10000
const INACTIVE_GAME_POLLING_INTERVAL_MS = 60000
const DRINK_ANIMATION_DURATION_MS = 5000
const HUB_URL = '/hub'

const replaceTemplatedStrings = (lobbyEvent: LobbyEvent) => parseLobbyEventText(lobbyEvent, lobby.value!, games.value)

const lobbyEventHandlers = {
  onDrinkAssigned: function (lobbyEvent: LobbyEvent) {
    if (lobbyEvent.lobbyMember2Id === currentLobbyMember.value?.id) notifyCurrentUserOfDrink(lobbyEvent)
  },
  onDrinkAwarded: function (lobbyEvent: LobbyEvent) {
    if (lobbyEvent.lobbyMemberId === currentLobbyMember.value?.id) notifyCurrentUserOfCorrectPick(lobbyEvent)
  }
}

//data
const store = useLobbyStore()
const route = useRoute()
const router = useRouter()
const toast = useToast()
const { lobby, currentUserId, events } = storeToRefs(store)
const { getLobby, getLobbyEvents, addLobbyEvent, addMessageToStore } = store
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

let hubConnection: SignalR.HubConnection

//computed
const feedItems = computed(getFeedItems)
const notificationPermissionsGranted = computed(() => Notification.permission === 'granted')
const selectedDateString = computed(() => (lobby.value ? format(addHours(lobby.value.created, -4), 'yyyy-MM-dd') : '1900-01-01'))
const currentLobbyMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const loadingMessage = computed(() => LoadingMessages.random())
const isLobbyView = computed(() => currentView.value === 'lobby')
const isFeedView = computed(() => currentView.value === 'feed')
const isGameView = computed(() => currentView.value === 'game')
const isChatView = computed(() => currentView.value === 'chat')
const pendingDrinkCount = computed(
  () => currentLobbyMember.value?.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)).length ?? 0
)
const messages = computed(() => {
  if (!lobby.value) return []

  return lobby.value.members
    .reduce(
      (messages: MessageViewModel[], member) => [
        ...messages,
        ...member.messages?.map((msg) => new MessageViewModel(member, msg.message, msg.sent, msg.id)) ?? []
      ],
      []
    )
    .sort((a, b) => compareAsc(a.sent, b.sent))
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

      while (name !== null && lobby.value?.members.some(m => m.name.toLowerCase() === name?.toLowerCase()))
        name = prompt('Sorry, that name is taken. Try another...')

      if (!name) return router.push({ name: 'Home' })

      await LobbyService.joinLobbyByCode(joinCode.value, name)
      await getLobby(joinCode.value)
    }

    if (!currentLobbyMember.value) return router.push({ name: 'Home' })
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({ joinCode: joinCode.value, name: currentLobbyMember.value.name }))

    await getLobbyEvents(lobby.value.id)
    await initializeHubConnection()
    await setGames()

    mappedEvents.value = events.value.map(replaceTemplatedStrings)
  } catch (e) {
    console.error(e)
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

async function setGames() {
  games.value = []

  const schedule = await NHL.getSchedule(selectedDateString.value)
  if (!schedule.games.length) return

  const gamePromises = schedule.games.map(async (game) => await getGameData(game.id))
  games.value = await Promise.all(gamePromises)
  games.value.forEach((game) => {
    if (!isGameStale(game)) timers.value.push(window.setTimeout(() => pollForUpdates(game), ACTIVE_GAME_POLLING_INTERVAL_MS))
  })
}

async function updateGame(gameId: number) {
  const gameIndex = games.value.findIndex((g) => gameId === g.id)
  games.value[gameIndex] = await getGameData(gameId)
}

async function getGameData(gameId: number) {
  return await NHL.getGame(gameId)
}

async function pollForUpdates(game: Game) {
  await updateGame(game.id)
  if (isGameStale(game)) return

  const interval = isGameInProgress(game) ? ACTIVE_GAME_POLLING_INTERVAL_MS : INACTIVE_GAME_POLLING_INTERVAL_MS

  timers.value.push(window.setTimeout(() => pollForUpdates(game), interval))
}

async function initializeHubConnection() {
  hubConnection = new SignalR.HubConnectionBuilder()
    .withUrl(HUB_URL, SignalR.HttpTransportType.LongPolling)
    .configureLogging(SignalR.LogLevel.Error)
    .withAutomaticReconnect()
    .build()

  hubConnection.on('LobbyEvent', dispatchLobbyEvent)
  hubConnection.on('Message', onNewMessage)

  try {
    await hubConnection.start()
    await hubConnection.invoke('JoinLobby', joinCode.value)
  } catch (err) {
    console.error(err)
  }
}

function onNewMessage(message: Message) {
  if (message.lobbyMemberId === currentLobbyMember.value?.id) return
  parseAllDates(message)
  addMessageToStore(message)

  if (currentView.value !== 'chat')
    unseenMessageCount.value++
}

function notifyCurrentUserOfDrink(lobbyEvent: LobbyEvent) {
  pendingDrinks.value.push(lobbyEvent)

  if (pendingDrinks.value.length === 1) processNextDrinkForCurrentUser()
}

function processNextDrinkForCurrentUser() {
  if (pendingDrinks.value.length === 0) return
  currentDrink.value = pendingDrinks.value[0]

  sendNotification('🍺 Drink!', {
    body: `Courtesy of ${getSenderNameByLobbyEvent(currentDrink.value)}`
  })

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
  if (lobbyEvent.lobbyEventType != LobbyEventType.NewPick || lobbyEvent.lobbyMemberId !== currentLobbyMember.value!.id) {
    await getLobby(joinCode.value)
    if (!lobby.value) return
  }

  if (lobbyEvent.lobbyEventType === LobbyEventType.UserRemoved && lobbyEvent.lobbyMemberId === currentLobbyMember.value!.id) {
    toast.error('You were removed from the lobby.')
    return router.push({ name: 'Home' })
  }

  parseAllDates(lobbyEvent)
  addLobbyEvent(lobbyEvent)
  mappedEvents.value.push(replaceTemplatedStrings(lobbyEvent))

  const eventType = LobbyEventType[lobbyEvent.lobbyEventType]
  const eventHandler = lobbyEventHandlers[eventType as keyof typeof lobbyEventHandlers]
  if (eventHandler) eventHandler(lobbyEvent)
}

function notifyCurrentUserOfCorrectPick(lobbyEvent: LobbyEvent) {
  const player = games.value.flatMap((g) => Object.values(g.playerSummaries)).find((p) => p.id === lobbyEvent.playerId)

  const playerMsg = player ? ` for a goal by ${player.firstName} ${player.lastName}` : ''
  const msg = `Give out a drink${playerMsg}!`
  toast.success(msg)

  sendNotification(`🚨 Give out a drink!`, { body: msg })
}

function sendNotification(text: string, options: {} | null = null) {
  if (!('Notification' in window)) return
  if (!notificationPermissionsGranted.value) return

  if (options) new Notification(text, options)
  else new Notification(text)
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
        return [...items, FeedItem.fromPlay(game.id, { away: game.awayTeam, home: game.homeTeam}, play, game.playerSummaries)]
      }
      else return items
    }, [])
  }
  )

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
    window.setTimeout(() => isInitialLoad.value = false, 2000)
    return
  }

  shouldAnimateFeed.value = true
  window.clearTimeout(feedAnimationTimer.value)
  window.setTimeout(() => shouldAnimateFeed.value = false, 700)
}

//watch 
watch(() => feedItems.value.length, (newLength, oldLength) => {
  if (newLength > oldLength) {
    animateFeed()
  }
})
</script>

<template>
  <div class="d-flex overflow-hidden flex-column" style="height: 100%">
    <template v-if="!isInvalidLobby">
      <VInstructionsModal v-if="isInstructionsVisible" :join-code="lobby?.joinCode" @close="isInstructionsVisible = false" />

      <div class="bg-stone-900 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center" style="z-index: 10">
        <router-link to="/" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
          <img src="/img/logo-wide.png" />
        </router-link>

        <a class="d-flex ms-auto pt-1 text-stone-0 fw-bold text-decoration-none" role="button" @click="isInstructionsVisible = true">
          <i class="fi fi-rr-question-square d-block ms-4"></i>
          <span class="text-uppercase ms-2 d-block" style="margin-top: -2px">How To Play</span>
        </a>
      </div>

      <div class="d-flex flex-grow-1 overflow-hidden bg-stone-800">
        <template v-if="!isLoading">
          <VGameScoreboards class="full-scoreboard flex-grow-1" :class="{ 'hide-mobile': !isGameView }" :games="games" style="overflow: auto" />

          <div class="feed flex-shrink-0 d-flex flex-column" :class="{ 'hide-mobile': !isFeedView && !isLobbyView && !isChatView }" style="width: 400px">
            <VLobbyOverview ref="overview" class="lobby-overview flex-shrink-0 v-lobby-overview  flex-grow-1" :class="{ 'hide-mobile': !isLobbyView }" />
            <VFeed class="flex-grow-1 v-feed" :items="feedItems" :class="{ 'hide-mobile': !isFeedView, 'animate': shouldAnimateFeed }" />
            <VChat ref="vChat" :messages="messages" class="flex-grow-1 v-chat" :class="{ 'hide-mobile': !isChatView }"></VChat>
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
@/services/NhlService