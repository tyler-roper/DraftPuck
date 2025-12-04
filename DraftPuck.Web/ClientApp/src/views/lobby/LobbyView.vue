<script setup lang="ts">
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { computed, nextTick, ref, watch, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import { compareAsc } from 'date-fns'
import { parseAllDates } from '@/helpers/dateHelpers'
import LobbyEventType from '@/enums/lobbyEventType'
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
import GameState from '@/enums/gameState'
import { useUserStore } from '@/stores/user'
import { useSystemStore } from '@/stores/system'
import VDrinkAnimation from '@/components/VDrinkAnimation.vue'
import VBottomNav from '@/components/VBottomNav.vue'
import VLobbyHeader from '@/components/VLobbyHeader.vue'

// Composables
import { useSignalRConnection } from '@/composables/useSignalRConnection'
import { useGamePolling } from '@/composables/useGamePolling'
import { useDrinkNotifications } from '@/composables/useDrinkNotifications'
import { useDebugCommands } from '@/composables/useDebugCommands'
import { useFeedItems } from '@/composables/useFeedItems'
import { useLobbyConnection } from '@/composables/useLobbyConnection'
import { createLobbyEventHandlers } from '@/utils/lobbyEventHandlers'

// Types
type View = 'feed' | 'game' | 'lobby' | 'chat' | 'picks'

// Constants
const LOCAL_STORAGE_KEY = 'latestLobby'

// Stores
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

// Local State
const joinCode = ref(route.params.joinCode as string)
const isInstructionsVisible = ref(false)
const isNotificationSettingsVisible = ref(false)
const isInvalidLobby = ref(false)
const isLoading = ref(true)
const currentView = ref<View>('picks')
const unseenMessageCount = ref(0)
const unseenMentionsCount = ref(0)
const vChat = ref<InstanceType<typeof VChat> | null>(null)
const shouldAnimateFeed = ref(false)
const feedAnimationTimer = ref<number>()
const isInitialLoad = ref(true)
const selectedGame = ref<Game>()

// Composables
const { hubConnection, initializeHubConnection, stopHubConnection } = useSignalRConnection(sendDebugMessage)
const { games, setGames, stopPolling } = useGamePolling(sendDebugMessage)
const { currentDrink, addDrinkForCurrentUser } = useDrinkNotifications()
const { feedItems } = useFeedItems(lobby, games, events, appIsTestMode)

const { initializeLobbyConnection, pauseLobbyConnection, updateLastLobbyRetrieval } = useLobbyConnection(
  sendDebugMessage,
  () => initializeHub(),
  setGames,
  getLobby,
  getLobbyEvents,
  stopHubConnection,
  stopPolling,
  lobby
)

// Computed
const currentLobbyMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const loadingMessage = computed(() => LoadingMessages.random())
const isLobbyView = computed(() => currentView.value === 'lobby')
const isFeedView = computed(() => currentView.value === 'feed')
const isPicksView = computed(() => currentView.value === 'picks')
const isGameView = computed(() => currentView.value === 'game')
const isChatView = computed(() => currentView.value === 'chat')
const sortedGames = computed(() =>
  !games.value
    ? []
    : [...games.value].sort((a, b) => {
        if (a.gameState === GameState.Final) return 1
        if (b.gameState === GameState.Final) return -1
        if (lobby.value?.gameIds.includes(a.id) && !lobby.value?.gameIds.includes(b.id)) return -1
        if (!lobby.value?.gameIds.includes(a.id) && lobby.value?.gameIds.includes(b.id)) return 1
        return compareAsc(a.dateTime, b.dateTime)
      })
)

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

const drinkSenderName = computed(() => {
  if (!currentDrink.value) return undefined
  return lobby.value?.members.find((m) => m.id === currentDrink.value!.lobbyMemberId)?.name
})

// Debug commands context
const debugCommandsContext = computed(() => ({
  lobby: lobby.value,
  currentLobbyMember: currentLobbyMember.value,
  hubConnection: hubConnection.value,
  setDebugging,
  sendSystemMessage,
  sendDebugMessage
}))

const { handleCommand } = useDebugCommands(debugCommandsContext)

// Lifecycle
onMounted(async () => {
  try {
    isLoading.value = true
    await userStore.initialize()
    await getLobby()

    if (!lobby.value) return (isInvalidLobby.value = true)
    if (!lobby.value.isActive) {
      isInvalidLobby.value = true
      router.replace({ name: 'LobbyReview', params: { joinCode: joinCode.value } })
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

    await initializeLobbyConnection(true)

    document.removeEventListener('visibilitychange', handleVisibilityChange)
    document.addEventListener('visibilitychange', handleVisibilityChange)
  } catch (e) {
    console.error(e)
    sendDebugMessage(e as string, 3)
    isInvalidLobby.value = true
  } finally {
    isLoading.value = false
  }
})

onUnmounted(pauseLobbyConnection)

// Methods
function handleVisibilityChange() {
  if (document.hidden) pauseLobbyConnection()
  else initializeLobbyConnection()
}

function setView(view: View) {
  currentView.value = view
}

async function getLobby() {
  if (!joinCode.value) return
  updateLastLobbyRetrieval()
  await getLobbyFromStore(joinCode.value)
  sendDebugMessage(`Got lobby ${joinCode.value}.`, 2)
}

async function initializeHub() {
  await initializeHubConnection(joinCode.value, currentLobbyMember.value, dispatchLobbyEvent, onNewMessage, getLobby)
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
  const eventHandlers = createLobbyEventHandlers({
    lobby: lobby.value,
    currentLobbyMember: currentLobbyMember.value,
    games: games.value,
    toast,
    onDrinkAssignedToCurrentUser: addDrinkForCurrentUser
  })

  const eventHandler = eventHandlers[`on${eventType}` as keyof typeof eventHandlers]
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

// Watchers
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

      <VLobbyHeader
        :join-code="lobby?.joinCode"
        @copy-invite="copyInvite"
        @open-instructions="isInstructionsVisible = true"
        @open-notification-settings="isNotificationSettingsVisible = true"
      />

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
            <VChat ref="vChat" :messages="messages" class="flex-grow-1 v-chat" :class="{ 'hide-mobile': !isChatView }" @command="handleCommand"></VChat>
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

      <VBottomNav
        v-if="!isLoading"
        :current-view="currentView"
        :pending-drink-count="pendingDrinkCount"
        :unseen-message-count="unseenMessageCount"
        :unseen-mentions-count="unseenMentionsCount"
        @set-view="setView"
        @set-view-to-chat="setViewToChat"
      />
    </template>

    <template v-if="isInvalidLobby">
      <div style="width: 100%; height: 100%" class="d-flex align-items-center">
        <div class="mx-auto d-flex flex-column align-items-center">
          <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase fw-bold">Sorry, this lobby is invalid.</span>
        </div>
      </div>
    </template>

    <VDrinkAnimation :sender-name="drinkSenderName" />
  </div>
</template>
