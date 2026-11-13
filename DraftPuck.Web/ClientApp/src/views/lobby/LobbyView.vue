<script setup lang="ts">
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { computed, nextTick, ref, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { TYPE, useToast } from 'vue-toastification'
import { compareAsc, addHours, isWithinInterval } from 'date-fns'
import LobbyEventType from '@/enums/lobbyEventType'
import { parseAllDates } from '@/helpers/dateHelpers'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'
import { buildFeedItems } from '@/helpers/feedHelpers'
import { sortGamesForLobby } from '@/helpers/gameHelpers'
import { useGamesStore } from '@/stores/games'
import { useLobbyConnection } from '@/composables/useLobbyConnection'
import LoadingMessages from '@/models/loadingMessages'
import VInstructionsModal from '@/components/VInstructionsModal.vue'
import VGameScoreboards from '@/components/VGameScoreboards.vue'
import VLobbyOverview from '@/components/VLobbyOverview.vue'
import VScoresRibbon from '@/components/VScoresRibbon.vue'
import VFeed from '@/components/VFeed.vue'
import VPicks from '@/components/VPicks.vue'
import MessageViewModel from '@/models/messageViewModel'
import VChat from '@/components/VChat.vue'
import VNotificationSettingsModal from '@/components/VNotificationSettingsModal.vue'
import LobbyService from '@/services/LobbyService'
import VUser from '@/components/VUser.vue'
import { useUserStore } from '@/stores/user'
import { useSystemStore } from '@/stores/system'
import VHtmlToast from '@/components/VHtmlToast.vue'
import type { ToastOptions } from 'vue-toastification/dist/types/types'

//const
type View = 'feed' | 'game' | 'lobby' | 'chat' | 'picks'
const LOCAL_STORAGE_KEY = 'latestLobby'
const DRINK_ANIMATION_DURATION_MS = 5000

const replaceTemplatedStrings = (lobbyEvent: LobbyEvent) => parseLobbyEventText(lobbyEvent, lobby.value!, games.value)

const lobbyEventHandlers: { [k: string]: (lobbyEvent: LobbyEvent) => void } = {
  onDrinkAssigned: notifyDrinkAssigned,
  onDrinkAwarded: notifyDrinkAwarded,
  onUserJoined: notifyUserJoined
}

function notifyDrinkAssigned(lobbyEvent: LobbyEvent) {
  var currentUserIsRecipient = lobbyEvent.lobbyMember2Id === currentLobbyMember.value?.id
  if (!currentUserIsRecipient) {
    lobbyEventToast(lobbyEvent)
    return
  }

  // "queue up" drink for current user
  pendingDrinksForCurrentUser.value.push(lobbyEvent)
  if (pendingDrinksForCurrentUser.value.length === 1) processNextDrinkForCurrentUser()
}

function notifyDrinkAwarded(lobbyEvent: LobbyEvent) {
  var isCurrentLobbyMember = lobbyEvent.lobbyMemberId === currentLobbyMember.value?.id
  lobbyEventToast(lobbyEvent, { type: isCurrentLobbyMember ? TYPE.SUCCESS : TYPE.INFO })
}

function notifyUserJoined(lobbyEvent: LobbyEvent) {
  lobbyEventToast(lobbyEvent)
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
    sendSystemMessage(JSON.stringify(connection.describe(), undefined, 2))
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
const userStore = useUserStore()
const systemStore = useSystemStore()
const { isLoggedIn, currentUser } = storeToRefs(userStore)
const store = useLobbyStore()
const route = useRoute()
const router = useRouter()
const toast = useToast()
const { lobby, currentUserId, events, systemMessages } = storeToRefs(store)
const { appIsTestMode } = storeToRefs(systemStore)
const { getLobby: getLobbyFromStore, getLobbyEvents, addLobbyEvent, addMessageToStore, sendDebugMessage, sendSystemMessage, setDebugging } = store
const joinCode = ref(route.params.joinCode as string)
const gamesStore = useGamesStore()
const { games } = storeToRefs(gamesStore)
const isInstructionsVisible = ref(false)
const isNotificationSettingsVisible = ref(false)
const isInvalidLobby = ref(false)
const isLoading = ref(true)
const mappedEvents = computed(() => events.value.map(e => replaceTemplatedStrings(e)));
const currentView = ref<View>('picks')
const pendingDrinksForCurrentUser = ref<LobbyEvent[]>([])
const currentDrink = ref<LobbyEvent>()
const unseenMessageCount = ref(0)
const unseenMentionsCount = ref(0)
const vChat = ref<InstanceType<typeof VChat> | null>(null)
const shouldAnimateFeed = ref(false)
const feedAnimationTimer = ref<number>()
const isInitialLoad = ref(true)
const selectedGame = ref<Game>()

// Live updates: lobby events, chat messages and game scores are pushed; resync fills in anything missed while away.
const connection = useLobbyConnection({
  joinCode: joinCode.value,
  handlers: {
    LobbyEvent: dispatchLobbyEvent,
    Message: onNewMessage,
    LobbyStateChanged: getLobby,
    GameUpdated: onGameUpdated
  },
  resync,
  log: sendDebugMessage
})

//computed
const feedItems = computed(() =>
  lobby.value ? buildFeedItems(games.value, mappedEvents.value, lobby.value.created, appIsTestMode.value) : []
)
const currentLobbyMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const loadingMessage = computed(() => LoadingMessages.random())
const isLobbyView = computed(() => currentView.value === 'lobby')
const isFeedView = computed(() => currentView.value === 'feed')
const isPicksView = computed(() => currentView.value === 'picks')
const isGameView = computed(() => currentView.value === 'game')
const isChatView = computed(() => currentView.value === 'chat')
const sortedGames = computed(() => sortGamesForLobby(games.value, lobby.value?.gameIds ?? []))

const is4Nations = computed(() => {
  const today = new Date()
  const firstDay = new Date(2025, 1, 10)
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
onMounted(async () => {
  try {
    isLoading.value = true
    await userStore.initialize()
    await getLobby()

    if (!lobby.value) return (isInvalidLobby.value = true)
    if (!lobby.value.isActive) {
      isInvalidLobby.value = true
      router.replace({ name: 'LobbyReview', params: { joinCode: joinCode.value }})
      return
    }

    let name: string | null = ''

    if (!currentLobbyMember.value) {
      if (!isLoggedIn.value) {
        name = prompt('Welcome! Choose a name...')

        while (name !== null && lobby.value?.members.some((m) => m.name.toLowerCase() === name?.toLowerCase()))
          name = prompt('Sorry, that name is taken. Try another...')

        if (!name) return router.push({ name: 'Home' })
      } else {
        name = currentUser.value!.nickname!
      }

      await LobbyService.joinLobbyByCode(joinCode.value, name)
      await getLobby()
    }

    if (!currentLobbyMember.value) return router.push({ name: 'Home' })
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({ joinCode: joinCode.value, name: currentLobbyMember.value.name }))

    await connection.start()
  } catch (e) {
    reportError(e)
    isInvalidLobby.value = true
  } finally {
    isLoading.value = false
  }
})

/** Everything the page shows that may have changed while it wasn't listening. */
async function resync() {
  await Promise.all([getLobby(), gamesStore.load()])
  if (lobby.value) await getLobbyEvents(lobby.value.id)
}

function onGameUpdated(game: Game) {
  parseAllDates(game)
  gamesStore.applyUpdate(game)
}

function lobbyEventToast(lobbyEvent: LobbyEvent, options?: ToastOptions | undefined) {
  return toast(
    {
      component: VHtmlToast,
      props: {
        title: lobbyEvent.title,
        message: replaceTemplatedStrings(lobbyEvent).text
      }
    },
    options
  )
}

function setView(view: View) {
  currentView.value = view
}

function reportError(error: unknown) {
  console.error(error)
  sendDebugMessage(String(error), 3)
}

function onNewMessage(message: Message) {
  if (message.lobbyMemberId === currentLobbyMember.value?.id) return
  parseAllDates(message)
  addMessageToStore(message)

  if (currentView.value !== 'chat') {
    const currentUserIsMentioned = message.message
      .split(' ')
      .some((word) => word.toUpperCase() === `@${currentLobbyMember.value?.name.toUpperCase()}`)
    if (currentUserIsMentioned) unseenMentionsCount.value++
    unseenMessageCount.value++
  }
}

function processNextDrinkForCurrentUser() {
  if (pendingDrinksForCurrentUser.value.length === 0) return
  currentDrink.value = pendingDrinksForCurrentUser.value[0]

  window.setTimeout(async () => {
    pendingDrinksForCurrentUser.value.splice(0, 1)
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

  if (
    [LobbyEventType.UserRemoved, LobbyEventType.UserLeft].includes(lobbyEvent.lobbyEventType) &&
    lobbyEvent.lobbyMemberId === currentLobbyMemberValue.id
  ) {
    const text = lobbyEvent.lobbyEventType === LobbyEventType.UserRemoved ? 'You were removed from the lobby.' : 'You left the lobby.'

    toast.error(text)
    router.replace('/')
    return
  }

  if (lobbyEvent.lobbyEventType != LobbyEventType.NewPick || lobbyEvent.lobbyMemberId !== currentLobbyMemberValue.id) {
    await getLobby()
    if (!lobby.value) return
  }

  parseAllDates(lobbyEvent)
  addLobbyEvent(lobbyEvent)

  const eventType = LobbyEventType[lobbyEvent.lobbyEventType]
  const eventHandler = lobbyEventHandlers[`on${eventType}`]

  if (eventHandler) eventHandler(lobbyEvent)
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
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { messages, picks, ...lobbyMemberInfo } = lobbyMember
  return lobbyMemberInfo
}

async function getLobby() {
  if (!joinCode.value) return
  await getLobbyFromStore(joinCode.value)
  sendDebugMessage(`Got lobby ${joinCode.value}.`, 2)
}

async function copyInvite() {
  const code = lobby.value!.joinCode
  try {
    await navigator.clipboard.writeText(`Join my DRAFTPUCK lobby! Code: ${code}\n\nhttps://draftpuck.com/lobby/${code}`)
    toast.success('Copied invite to clipboard!', { timeout: 2000 })
  } catch {
    toast.error('Cannot copy')
  }
}

async function selectGame(game: Game) {
  if (!isPicksView.value) setView('picks')

  await nextTick()

  if (game === undefined) {
    selectedGame.value = undefined
    return
  }

  if (selectedGame.value?.id === game.id) return

  selectedGame.value = game
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
      <VNotificationSettingsModal v-if="isNotificationSettingsVisible" @close="isNotificationSettingsVisible = false" />
      <div class="bg-stone-900 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center justify-content-between" style="z-index: 10">
        <router-link :to="{ name: 'Home' }" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
          <img v-if="!is4Nations" src="/img/logo-wide.png" />
          <img v-if="is4Nations" src="/img/logo-wide-4nations.png" />
        </router-link>

        <a
          @click="copyInvite"
          role="button"
          class="d-block ms-auto bg-primary text-stone-900 px-2 rounded text-decoration-none fs-7 ms-1 d-flex align-items-center"
          v-if="lobby?.joinCode !== undefined"
        >
          <span class="fs-5 me-1 fw-bold" style="letter-spacing: 3px">{{ lobby?.joinCode }}</span>
          <i class="fi fi-sr-share d-block mb-n1 d-block"></i>
        </a>

        <a
          class="d-flex ms-auto me-sm-5 me-3 pt-1 text-white fw-bold text-decoration-none align-items-center"
          role="button"
          @click="isNotificationSettingsVisible = true"
        >
          <i class="fi fi-rr-settings d-block fs-3" style="line-height: 20px"></i>
          <span class="d-none d-sm-block text-uppercase ms-2" style="margin-top: -2px">Notifications</span>
        </a>

        <a class="d-flex pt-1 text-stone-0 fw-bold text-decoration-none align-items-center" role="button" @click="isInstructionsVisible = true">
          <i class="fi fi-rr-question-square d-block fs-3" style="line-height: 20px"></i>
          <span class="d-none d-sm-block text-uppercase ms-2" style="margin-top: -2px">How To Play</span>
        </a>

        <VUser class="ms-3" display="avatar" :avatar-size-in-px="30" :show-menu-on-click="true" />
      </div>

      <div class="d-flex flex-grow-1 overflow-hidden bg-stone-800">
        <template v-if="!isLoading">
          <VGameScoreboards class="full-scoreboard flex-grow-1" :class="{ 'hide-mobile': !isGameView }" :games="sortedGames" style="overflow: auto" />

          <div
            class="feed flex-shrink-0 d-flex flex-column"
            :class="{ 'hide-mobile': !isFeedView && !isLobbyView && !isChatView && !isPicksView }"
            style="width: 400px"
          >
            <VScoresRibbon class="d-sm-none" :games="sortedGames" :selected-game="selectedGame" @on-score-clicked="selectGame" />
            <VLobbyOverview ref="overview" class="lobby-overview v-lobby-overview flex-grow-1" :class="{ 'hide-mobile': !isLobbyView }" />
            <VFeed class="flex-grow-1 v-feed" :items="feedItems" :class="{ 'hide-mobile': !isFeedView, animate: shouldAnimateFeed }" />
            <VPicks
              ref="vPicks"
              @select-game="selectGame"
              class="flex-grow-1 v-picks d-sm-none"
              :selected-game="selectedGame"
              :games="sortedGames"
              :class="{ 'hide-mobile': !isPicksView }"
            />
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
        <a role="button" class="text-center p-2 text-white" :class="{ active: isPicksView }" @click="setView('picks')">
          <i class="fi fi-rs-hockey-mask"></i><br />
          <span>PICKS</span>
        </a>
        <a role="button" class="text-center p-2 text-white d-none" :class="{ active: isGameView }" @click="setView('game')">
          <i class="fi fi-rr-hockey-puck"></i><br />
          <span>SCORES</span>
        </a>
        <a role="button" class="text-center p-2 text-white" :class="{ active: isChatView }" @click="setViewToChat">
          <i v-if="unseenMessageCount <= 0" class="fi fi-rr-comment-alt"></i>
          <span v-if="unseenMessageCount > 0" class="drink-badge" :class="{ 'bg-primary': unseenMentionsCount > 0 }">
            <span v-if="unseenMentionsCount > 0">📢</span>
            <span v-if="unseenMentionsCount <= 0">💬</span>
            <span>{{ unseenMessageCount }}</span>
          </span>
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
