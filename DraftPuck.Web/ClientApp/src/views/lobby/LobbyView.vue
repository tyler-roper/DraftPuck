<script setup lang="ts">
import { computed, nextTick, ref, watch, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter, useRoute } from 'vue-router'
import { TYPE, useToast } from 'vue-toastification'
import { useLobbyStore } from '@/stores/lobby'
import { useGamesStore } from '@/stores/games'
import { useUserStore } from '@/stores/user'
import { useSystemStore } from '@/stores/system'
import { useLobbyConnection } from '@/composables/useLobbyConnection'
import { useLobbyEvents, type LobbyEventTone } from '@/composables/useLobbyEvents'
import { useLobbyChat } from '@/composables/useLobbyChat'
import { useDrinkQueue } from '@/composables/useDrinkQueue'
import { parseAllDates } from '@/helpers/dateHelpers'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'
import { buildFeedItems } from '@/helpers/feedHelpers'
import { sortGamesForLobby } from '@/helpers/gameHelpers'
import { runChatCommand } from '@/helpers/chatCommands'
import LobbyService from '@/services/LobbyService'
import LoadingMessages from '@/models/loadingMessages'
import VInstructionsModal from '@/components/VInstructionsModal.vue'
import VGameScoreboards from '@/components/VGameScoreboards.vue'
import VLobbyOverview from '@/components/VLobbyOverview.vue'
import VScoresRibbon from '@/components/VScoresRibbon.vue'
import VFeed from '@/components/VFeed.vue'
import VPicks from '@/components/VPicks.vue'
import VChat from '@/components/VChat.vue'
import VNotificationSettingsModal from '@/components/VNotificationSettingsModal.vue'
import VJoinLobbyForm from '@/components/VJoinLobbyForm.vue'
import VLogo from '@/components/VLogo.vue'
import VUser from '@/components/VUser.vue'
import VHtmlToast from '@/components/VHtmlToast.vue'

type View = 'feed' | 'game' | 'lobby' | 'chat' | 'picks'
// Remembered so the home page's join and create forms can prefill the code and name.
const LOCAL_STORAGE_KEY = 'latestLobby'
const DRINK_ANIMATION_DURATION_MS = 5000

//#region stores
const route = useRoute()
const router = useRouter()
const toast = useToast()
const userStore = useUserStore()
const store = useLobbyStore()
const gamesStore = useGamesStore()
const { isLoggedIn, currentUser } = storeToRefs(userStore)
const { lobby, currentMember, events, chatMessages } = storeToRefs(store)
const { appIsTestMode } = storeToRefs(useSystemStore())
const { games } = storeToRefs(gamesStore)
const { getLobbyEvents, sendDebugMessage, sendSystemMessage, setDebugging } = store
//#endregion

//#region page state
const joinCode = route.params.joinCode as string
const isLoading = ref(true)
const isInvalidLobby = ref(false)
// A guest opened the lobby's link without being a member: ask for their name here, then join.
const isAskingForName = ref(false)
const loadingMessage = LoadingMessages.random()
const currentView = ref<View>('picks')
const selectedGame = ref<Game>()
const isInstructionsVisible = ref(false)
const isNotificationSettingsVisible = ref(false)
const vChat = ref<InstanceType<typeof VChat> | null>(null)
const shouldAnimateFeed = ref(false)
const isInitialLoad = ref(true)
//#endregion

//#region live updates
const { current: currentDrink, enqueue: showDrinkForMe } = useDrinkQueue(DRINK_ANIMATION_DURATION_MS)
const { unreadCount, unreadMentionCount, onMessage, markRead } = useLobbyChat(() => currentView.value === 'chat')
const { onLobbyEvent } = useLobbyEvents({
  refreshLobby: getLobby,
  showDrinkForMe,
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
  handlers: { LobbyEvent: onLobbyEvent, Message: onMessage, LobbyStateChanged: getLobby, GameUpdated: onGameUpdated },
  resync,
  log: sendDebugMessage
})
//#endregion

//#region computed
const templatedEvents = computed(() => events.value.map(fillEventTemplate))
const feedItems = computed(() =>
  lobby.value ? buildFeedItems(games.value, templatedEvents.value, lobby.value.created, appIsTestMode.value) : []
)
const sortedGames = computed(() => sortGamesForLobby(games.value, lobby.value?.gameIds ?? []))
const pendingDrinkCount = computed(() => currentMember.value?.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)).length ?? 0)
const isLobbyView = computed(() => currentView.value === 'lobby')
const isFeedView = computed(() => currentView.value === 'feed')
const isPicksView = computed(() => currentView.value === 'picks')
const isGameView = computed(() => currentView.value === 'game')
const isChatView = computed(() => currentView.value === 'chat')
const isLobbyReady = computed(() => !isLoading.value && !isAskingForName.value)
//#endregion

//#region loading and joining
onMounted(async () => {
  try {
    await userStore.initialize()
    await getLobby()

    if (!lobby.value) return (isInvalidLobby.value = true)
    if (!lobby.value.isActive) {
      isInvalidLobby.value = true
      router.replace({ name: 'LobbyReview', params: { joinCode } })
      return
    }

    if (!currentMember.value) {
      // Logged-in users join under their nickname; guests choose a name first (the join form calls onJoined).
      if (!isLoggedIn.value) return (isAskingForName.value = true)
      await LobbyService.joinLobbyByCode(joinCode, currentUser.value!.nickname!)
      await getLobby()
    }

    await enterLobby()
  } catch (e) {
    reportError(e)
    isInvalidLobby.value = true
  } finally {
    isLoading.value = false
  }
})

async function onJoined() {
  isAskingForName.value = false
  isLoading.value = true
  try {
    await getLobby()
    await enterLobby()
  } catch (e) {
    reportError(e)
    isInvalidLobby.value = true
  } finally {
    isLoading.value = false
  }
}

async function enterLobby() {
  if (!currentMember.value) return router.push({ name: 'Home' })
  localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({ joinCode, name: currentMember.value.name }))
  await connection.start()
}

async function getLobby() {
  await store.getLobby(joinCode)
  sendDebugMessage(`Got lobby ${joinCode}.`, 2)
}

/** Everything the page shows that may have changed while it wasn't listening. */
async function resync() {
  await Promise.all([getLobby(), gamesStore.load()])
  if (lobby.value) await getLobbyEvents(lobby.value.id)
}

function onGameUpdated(game: Game) {
  parseAllDates(game)
  gamesStore.applyUpdate(game)
}

function reportError(error: unknown) {
  console.error(error)
  sendDebugMessage(String(error), 3)
}
//#endregion

//#region lobby events and chat
function fillEventTemplate(lobbyEvent: LobbyEvent) {
  return parseLobbyEventText(lobbyEvent, lobby.value!, games.value)
}

function toastLobbyEvent(lobbyEvent: LobbyEvent, tone?: LobbyEventTone) {
  const type = tone === 'success' ? TYPE.SUCCESS : tone === 'info' ? TYPE.INFO : undefined
  toast({ component: VHtmlToast, props: { title: lobbyEvent.title, message: fillEventTemplate(lobbyEvent).text } }, type ? { type } : undefined)
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

async function openChat() {
  currentView.value = 'chat'
  markRead()
  await nextTick()
  vChat.value?.focus()
}
//#endregion

//#region page UI
function setView(view: View) {
  currentView.value = view
}

async function selectGame(game: Game | undefined) {
  if (!isPicksView.value) setView('picks')
  await nextTick()
  if (selectedGame.value?.id !== game?.id) selectedGame.value = game
}

async function copyInvite() {
  try {
    const code = lobby.value?.joinCode ?? joinCode
    await navigator.clipboard.writeText(`Join my DRAFTPUCK lobby! Code: ${code}\n\nhttps://draftpuck.com/lobby/${code}`)
    toast.success('Copied invite to clipboard!', { timeout: 2000 })
  } catch {
    toast.error('Cannot copy')
  }
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
    <template v-if="!isInvalidLobby">
      <VInstructionsModal v-if="isInstructionsVisible" :join-code="lobby?.joinCode" @close="isInstructionsVisible = false" />
      <VNotificationSettingsModal v-if="isNotificationSettingsVisible" @close="isNotificationSettingsVisible = false" />
      <div class="bg-stone-900 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center justify-content-between" style="z-index: 10">
        <router-link :to="{ name: 'Home' }" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
          <VLogo />
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
        <div v-if="isAskingForName" class="d-flex align-items-center justify-content-center w-100 p-3">
          <div class="bg-stone-900 rounded shadow p-4" style="width: 100%; max-width: 420px">
            <h2 class="fs-4 fw-bold text-uppercase mb-1">Join lobby {{ lobby?.joinCode ?? joinCode }}</h2>
            <p class="text-stone-400 mb-3">Choose a name to join as a guest.</p>
            <VJoinLobbyForm :code="lobby?.joinCode ?? joinCode" @joined="onJoined" />
          </div>
        </div>

        <template v-if="isLobbyReady">
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
              :messages="chatMessages"
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

      <div class="bottom-nav d-flex d-sm-none bg-stone-900 shadow fw-bold" v-if="isLobbyReady">
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
        <a role="button" class="text-center p-2 text-white" :class="{ active: isChatView }" @click="openChat">
          <i v-if="unreadCount <= 0" class="fi fi-rr-comment-alt"></i>
          <span v-if="unreadCount > 0" class="drink-badge" :class="{ 'bg-primary': unreadMentionCount > 0 }">
            <span v-if="unreadMentionCount > 0">📢</span>
            <span v-if="unreadMentionCount <= 0">💬</span>
            <span>{{ unreadCount }}</span>
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
          {{ senderName(currentDrink) }}
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
