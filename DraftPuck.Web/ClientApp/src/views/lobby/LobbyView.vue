<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from '@/composables/useToast'
import { useLobbyStore } from '@/stores/lobby'
import { useGamesStore } from '@/stores/games'
import { useSystemStore } from '@/stores/system'
import { useLobbyConnection } from '@/composables/useLobbyConnection'
import { useLobbyEntry } from '@/composables/useLobbyEntry'
import { useLobbyEvents } from '@/composables/useLobbyEvents'
import { useLobbyChat } from '@/composables/useLobbyChat'
import { useEventQueue } from '@/composables/useEventQueue'
import { parseAllDates } from '@/helpers/dateHelpers'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'
import { buildFeedItems } from '@/helpers/feedHelpers'
import { sortGamesForLobby } from '@/helpers/gameHelpers'
import { runChatCommand } from '@/helpers/chatCommands'
import { messageForDrinkEvent } from '@/helpers/drinkMessageHelpers'
import LoadingMessages from '@/models/loadingMessages'
import VInstructionsModal from '@/components/VInstructionsModal.vue'
import VNotificationSettingsModal from '@/components/VNotificationSettingsModal.vue'
import VShareLobbyModal from '@/components/VShareLobbyModal.vue'
import VLobbyHeader from '@/components/VLobbyHeader.vue'
import VLobbyGuestJoin from '@/components/VLobbyGuestJoin.vue'
import VLobbyBottomNav, { type LobbyView } from '@/components/VLobbyBottomNav.vue'
import VGameScoreboards from '@/components/VGameScoreboards.vue'
import VLobbyOverview from '@/components/VLobbyOverview.vue'
import VScoresRibbon from '@/components/VScoresRibbon.vue'
import VFeed from '@/components/VFeed.vue'
import VPicks from '@/components/VPicks.vue'
import VChat from '@/components/VChat.vue'
import VEventBanner from '@/components/VEventBanner.vue'
import VDrinkPour from '@/components/VDrinkPour.vue'

const DRINK_ANIMATION_DURATION_MS = 5000
const BANNER_DURATION_MS = 4500

//#region stores
const route = useRoute()
const router = useRouter()
const toast = useToast()
const store = useLobbyStore()
const gamesStore = useGamesStore()
const { lobby, currentMember, events, chatMessages } = storeToRefs(store)
const { appIsTestMode } = storeToRefs(useSystemStore())
const { games } = storeToRefs(gamesStore)
const { getLobbyEvents, sendDebugMessage, sendSystemMessage, setDebugging } = store
//#endregion

//#region page state
const joinCode = route.params.joinCode as string
const loadingMessage = LoadingMessages.random()
// Which panel shows on a phone; on wider screens they all show.
const currentView = ref<LobbyView>('picks')
const selectedGame = ref<Game>()
const openModal = ref<'instructions' | 'notifications' | 'share'>()
const vChat = ref<InstanceType<typeof VChat> | null>(null)
const shouldAnimateFeed = ref(false)
const isInitialLoad = ref(true)
//#endregion

//#region live updates
const { current: currentDrink, enqueue: showDrinkForMe } = useEventQueue(DRINK_ANIMATION_DURATION_MS)
// Banners (callouts, and drinks for others) queue separately: at the top, while a drink for you shows in the middle.
const { current: currentBanner, enqueue: showBanner } = useEventQueue(BANNER_DURATION_MS)
const { unreadCount, unreadMentionCount, onMessage, markRead } = useLobbyChat(() => currentView.value === 'chat')
const { onLobbyEvent } = useLobbyEvents({
  refreshLobby: getLobby,
  showDrinkForMe,
  showBanner,
  notify: toastLobbyEvent,
  onRemovedFromLobby: (message) => {
    toast.error(message)
    router.replace('/')
  },
  log: sendDebugMessage
})

// Lobby events, chat messages and game scores are pushed; resync fills in anything missed while not listening.
const connection = useLobbyConnection({
  joinCode,
  handlers: { LobbyEvent: onLobbyEvent, Message: onMessage, LobbyStateChanged: getLobby, GameChanged: onGameChanged },
  resync,
  log: sendDebugMessage
})

const { isLoading, isInvalidLobby, isAskingForName, isReady, onJoined } = useLobbyEntry({
  joinCode,
  refreshLobby: getLobby,
  connect: connection.start,
  reportError
})

async function getLobby() {
  await store.getLobby(joinCode)
  sendDebugMessage(`Got lobby ${joinCode}.`, 2)
}

/** Everything the page shows that may have changed while it wasn't listening. */
async function resync() {
  await Promise.all([getLobby(), gamesStore.load()])
  if (lobby.value) await getLobbyEvents(lobby.value.id)
}

function onGameChanged(changes: GameChanges) {
  parseAllDates(changes)
  gamesStore.applyChanges(changes).catch(reportError)
}

function reportError(error: unknown) {
  console.error(error)
  sendDebugMessage(String(error), 3)
}
//#endregion

//#region computed
const templatedEvents = computed(() => events.value.map(fillEventTemplate))
const feedItems = computed(() => (lobby.value ? buildFeedItems(games.value, templatedEvents.value, lobby.value.created, appIsTestMode.value) : []))
const sortedGames = computed(() => sortGamesForLobby(games.value, lobby.value?.gameIds ?? []))
const pendingDrinkCount = computed(() => currentMember.value?.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)).length ?? 0)
//#endregion

//#region lobby events and chat
function fillEventTemplate(lobbyEvent: LobbyEvent) {
  return parseLobbyEventText(lobbyEvent, lobby.value!, games.value)
}

function toastLobbyEvent(lobbyEvent: LobbyEvent) {
  toast.html(fillEventTemplate(lobbyEvent).text, { title: lobbyEvent.title })
}

function senderName(lobbyEvent: LobbyEvent) {
  return lobby.value?.members.find((m) => m.id === lobbyEvent.lobbyMemberId)?.name
}

function handleCommand(command: string, ...args: string[]) {
  runChatCommand(command, args, {
    lobby: lobby.value,
    currentMember: currentMember.value,
    setDebugging,
    sendSystemMessage,
    describeConnection: connection.describe
  })
}
//#endregion

//#region page UI
async function setView(view: LobbyView) {
  currentView.value = view
  if (view !== 'chat') return

  markRead()
  await nextTick()
  vChat.value?.focus()
}

async function selectGame(game: Game | undefined) {
  if (currentView.value !== 'picks') setView('picks')
  await nextTick()
  if (selectedGame.value?.id !== game?.id) selectedGame.value = game
}

// New feed items briefly animate the feed, except while it first fills up.
watch(
  () => feedItems.value.length,
  (newLength, oldLength) => {
    if (newLength <= oldLength) return
    if (isInitialLoad.value) {
      window.setTimeout(() => (isInitialLoad.value = false), 2000)
      return
    }
    shouldAnimateFeed.value = true
    window.setTimeout(() => (shouldAnimateFeed.value = false), 700)
  }
)
//#endregion
</script>

<template>
  <div class="d-flex overflow-hidden flex-column" style="height: 100%">
    <div v-if="isInvalidLobby" style="width: 100%; height: 100%" class="d-flex align-items-center">
      <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase fw-bold">Sorry, this lobby is invalid.</span>
    </div>

    <template v-else>
      <VInstructionsModal v-if="openModal === 'instructions'" :join-code="lobby?.joinCode" @close="openModal = undefined" />
      <VNotificationSettingsModal v-if="openModal === 'notifications'" :join-code="joinCode" @close="openModal = undefined" />
      <VShareLobbyModal
        v-if="openModal === 'share'"
        :join-code="lobby?.joinCode ?? joinCode"
        :member-user-ids="lobby?.members.map((m) => m.userId) ?? []"
        @close="openModal = undefined"
      />
      <VLobbyHeader
        :join-code="lobby?.joinCode"
        @share="openModal = 'share'"
        @notifications="openModal = 'notifications'"
        @instructions="openModal = 'instructions'"
      />

      <div class="d-flex flex-grow-1 overflow-hidden bg-stone-800">
        <VLobbyGuestJoin v-if="isAskingForName" :join-code="lobby?.joinCode ?? joinCode" @joined="onJoined" />

        <template v-if="isReady">
          <VGameScoreboards
            class="full-scoreboard flex-grow-1"
            :class="{ 'hide-mobile': currentView !== 'game' }"
            :games="sortedGames"
            style="overflow: auto"
          />

          <div class="feed flex-shrink-0 d-flex flex-column" :class="{ 'hide-mobile': currentView === 'game' }" style="width: 400px">
            <VScoresRibbon class="d-sm-none" :games="sortedGames" :selected-game="selectedGame" @on-score-clicked="selectGame" />
            <VLobbyOverview class="lobby-overview v-lobby-overview flex-grow-1" :class="{ 'hide-mobile': currentView !== 'lobby' }" />
            <VFeed class="flex-grow-1 v-feed" :items="feedItems" :class="{ 'hide-mobile': currentView !== 'feed', animate: shouldAnimateFeed }" />
            <VPicks
              @select-game="selectGame"
              class="flex-grow-1 v-picks d-sm-none"
              :selected-game="selectedGame"
              :games="sortedGames"
              :class="{ 'hide-mobile': currentView !== 'picks' }"
            />
            <VChat
              ref="vChat"
              :messages="chatMessages"
              class="flex-grow-1 v-chat"
              :class="{ 'hide-mobile': currentView !== 'chat' }"
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

      <VLobbyBottomNav
        v-if="isReady"
        :view="currentView"
        :pending-drink-count="pendingDrinkCount"
        :unread-count="unreadCount"
        :unread-mention-count="unreadMentionCount"
        @select="setView"
      />
    </template>

    <VEventBanner v-if="currentBanner" :key="currentBanner.id" :event="fillEventTemplate(currentBanner)" :duration-ms="BANNER_DURATION_MS" />

    <VDrinkPour
      v-if="currentDrink"
      :key="currentDrink.id"
      :sender-name="senderName(currentDrink) ?? 'Someone'"
      :message="lobby ? messageForDrinkEvent(lobby, currentDrink) : undefined"
      :duration-ms="DRINK_ANIMATION_DURATION_MS"
    />
  </div>
</template>
