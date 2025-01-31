<script setup lang="ts">
import LobbyEventType from '@/enums/lobbyEventType'
import FeedItem from '@/models/feedItem'
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { ref, computed } from 'vue'
import VSwitch from '@/components/VSwitch.vue'
import { parseISO, format } from 'date-fns'
import VFeedItem from '@/components/VFeedItem.vue'
import PlayType from '@/enums/playType'
import FeedItemType from '@/enums/feedItemType'
import { initializeApp } from 'firebase/app'
import { getToken, onMessage, getMessaging } from 'firebase/messaging'
import UserService from '@/services/UserService'

//const
const isNotificationsSupported = 'Notification' in window
type View = 'feed' | 'list' | 'settings'

//props
const props = withDefaults(
  defineProps<{
    items: FeedItem[]
  }>(),
  {
    items: () => []
  }
)

//data
const lobbyStore = useLobbyStore()
const { lobby, currentUserId } = storeToRefs(lobbyStore)
const notificationPermissionsGranted = ref(isNotificationsSupported && Notification.permission === 'granted')
const token = ref<string>()
const currentView = ref<View>('feed')
const filters = ref<{ [k: string]: boolean }>({
  showGoals: true,
  showPenalties: true,
  showPeriodStarts: true,
  showPeriodEnds: true,
  showChallenges: true,
  showGameEnds: true,
  showUserJoin: true,
  showNameChange: true,
  showPicks: true,
  showDrinkAwarded: true,
  showDrinkAssigned: true
})

const gameEventFilterLookup = ref({
  showGoals: PlayType.Goal,
  showPenalties: PlayType.Penalty,
  showPeriodStarts: PlayType.PeriodStart,
  showPeriodEnds: PlayType.PeriodEnd,
  showChallenges: PlayType.Challenge,
  showGameEnds: PlayType.GameEnd
})

const lobbyEventFilterLookup = ref({
  showUserJoin: LobbyEventType.UserJoined,
  showNameChange: LobbyEventType.UserNameChanged,
  showPicks: LobbyEventType.NewPick,
  showDrinkAwarded: LobbyEventType.DrinkAwarded,
  showDrinkAssigned: LobbyEventType.DrinkAssigned
})

const messaging = ref<any>(null)

//computed
const filteredItems = computed(() => {
  return props.items.filter((item, idx, array) => {
    const relevantFilterLookups = item.type === FeedItemType.GameEvent ? gameEventFilterLookup.value : lobbyEventFilterLookup.value

    const filterLookup = Object.entries(relevantFilterLookups).find(([_key, value]) => value === item.subType)
    const shouldShow = filterLookup ? filters.value[filterLookup[0]] : true

    const isDuplicate =
      array[idx + 1] && item.subType === PlayType.PeriodEnd && array[idx + 1].subType === PlayType.GameEnd && array[idx + 1].gameId === item.gameId

    return shouldShow && !isDuplicate
  })
})

const assignedDrinks = computed(() =>
  lobby
    .value!.members.flatMap((m) => m.picks)
    .flatMap((p) => p.drinks)
    .filter((d) => d.recipientLobbyMemberId)
    .sort((a, b) => Number(b.created) - Number(a.created))
)

const allGameEventsOn = computed(
  () =>
    filters.value.showGoals &&
    filters.value.showPenalties &&
    filters.value.showPeriodStarts &&
    filters.value.showPeriodEnds &&
    filters.value.showChallenges &&
    filters.value.showGameEnds
)

const allLobbyEventsOn = computed(
  () =>
    filters.value.showUserJoin &&
    filters.value.showNameChange &&
    filters.value.showPicks &&
    filters.value.showDrinkAwarded &&
    filters.value.showDrinkAssigned
)

//hooks/methods
;(function created() {
  initializeFilters()
  initializeFirebase()
})()

async function requestNotificationPermissions() {
  console.log('test')
  await Notification.requestPermission()
  notificationPermissionsGranted.value = Notification.permission === 'granted'

  if (notificationPermissionsGranted.value) fetchAndUpdateFcmToken()
  else clearFcmToken()
}

function initializeFilters() {
  const existingFilters = localStorage.getItem('feedFilters')
  if (existingFilters) filters.value = { ...filters.value, ...JSON.parse(existingFilters) }
}

async function initializeFirebase() {
  const firebaseConfig = {
    apiKey: 'AIzaSyBGw_anxN2MDnfPSTyvqmfmYAwKTdLBOAY',
    authDomain: 'draftpuck.firebaseapp.com',
    projectId: 'draftpuck',
    storageBucket: 'draftpuck.firebasestorage.app',
    messagingSenderId: '34141903027',
    appId: '1:34141903027:web:7d676e25fe00fcb582b8c6'
  }

  const app = initializeApp(firebaseConfig)
  messaging.value = getMessaging(app)

  onMessage(messaging.value, async ({ notification, ..._ }) => {
    console.log("RECEIVED", notification);
    if (!notification) return
    const registration = await navigator.serviceWorker.ready
    registration.showNotification(notification.title ?? "Notification", {
      body: notification?.body,
      icon: notification?.icon
    })
  })

  if (notificationPermissionsGranted.value) fetchAndUpdateFcmToken()
  else clearFcmToken()
}

async function clearFcmToken() {
  await UserService.updateFcmRegistrationToken(currentUserId.value!, { token: undefined })
  token.value = undefined
}

async function fetchAndUpdateFcmToken() {
  const _token = await getToken(messaging.value, {
    vapidKey: 'BOngebl5Rmrgo0k0YMjstWPapJ-Zl0Izbbsyl0l0lI7L9cmHiDdcLUEj3moGuibR_YxTfGYKC134nSB42ZxxTaA'
  })
  await UserService.updateFcmRegistrationToken(currentUserId.value!, { token: _token })
  token.value = _token
}

function saveFiltersToLocalStorage() {
  localStorage.setItem('feedFilters', JSON.stringify(filters.value))
}

function getNameByDrink(drink: Drink) {
  const member = lobby.value!.members.find((m) => m.picks.some((p) => p.id === drink.lobbyMemberPickId))
  return member?.name ?? ''
}

function getRecipientNameByDrink(drink: Drink) {
  return lobby.value!.members.find((m) => m.id === drink.recipientLobbyMemberId)?.name
}

function setView(view: View) {
  currentView.value = view
}

function showAllGameEvents() {
  filters.value = {
    ...filters.value,
    showGoals: true,
    showPenalties: true,
    showPeriodStarts: true,
    showPeriodEnds: true,
    showChallenges: true,
    showGameEnds: true
  }
  saveFiltersToLocalStorage()
}

function hideAllGameEvents() {
  filters.value = {
    ...filters.value,
    showGoals: false,
    showPenalties: false,
    showPeriodStarts: false,
    showPeriodEnds: false,
    showChallenges: false,
    showGameEnds: false
  }
  saveFiltersToLocalStorage()
}

function showAllLobbyEvents() {
  filters.value = {
    ...filters.value,
    showUserJoin: true,
    showNameChange: true,
    showPicks: true,
    showDrinkAwarded: true,
    showDrinkAssigned: true
  }
  saveFiltersToLocalStorage()
}

function hideAllLobbyEvents() {
  filters.value = {
    ...filters.value,
    showUserJoin: false,
    showNameChange: false,
    showPicks: false,
    showDrinkAwarded: false,
    showDrinkAssigned: false
  }
  saveFiltersToLocalStorage()
}

function formatAsTime(date: Date | string) {
  date = typeof date === 'string' ? parseISO(date) : date
  return format(date, 'p')
}
</script>

<template>
  <div style="overflow-y: scroll" class="bg-stone-300 text-stone-800 d-flex flex-column">
    <div class="bg-stone-150 py-1 px-3 ls-2 shadow d-flex align-items-center" style="z-index: 2; position: sticky; top: 0">
      <div class="fs-5 me-2">🚨</div>
      <div>
        <span class="d-block mb-n2">The</span>
        <span class="fs-6 fw-bold d-block text-uppercase">Feed</span>
      </div>
      <div class="ms-auto d-flex">
        <a
          role="button"
          v-if="currentView !== 'feed'"
          @click="setView('feed')"
          class="fs-6 p-3 text-stone-400 d-block m-n3 fw-bold text-uppercase"
          style="text-decoration: none !important"
          >Back To Feed</a
        >
        <a
          role="button"
          v-if="currentView === 'feed'"
          @click="setView('list')"
          class="p-3 text-stone-400 d-block my-n3 mx-3"
          style="text-decoration: none !important"
          ><span class="fw-bold text-uppercase">Timeline</span></a
        >
        <a
          role="button"
          v-if="currentView === 'feed'"
          @click="setView('settings')"
          class="p-3 text-stone-400 d-block m-n3"
          style="text-decoration: none !important"
        >
          <span class="fw-bold text-uppercase">Settings</span>
        </a>
      </div>
    </div>

    <div class="d-flex flex-column-reverse flex-grow-1" style="justify-content: flex-end">
      <div v-if="currentView === 'settings'" class="flex-grow-1 bg-stone-100">
        <div class="fw-bold text-uppercase text-center border py-2 bg-stone-0" style="border-bottom: none !important">Feed Settings</div>
        <div class="pt-4 px-4">
          <span class="d-block fs-6 fw-bold">Push Notifications</span>
          <div class="ms-3" v-if="isNotificationsSupported">
            <button v-if="!notificationPermissionsGranted" @click="requestNotificationPermissions" class="btn btn-primary fw-bold text-uppercase">
              Enable Notifications
            </button>
            <span v-if="notificationPermissionsGranted && !!token"
              ><strong>You've enabled push notifications!</strong><br />You can disable them through your browser.</span
            >
          </div>
          <div class="ms-3" v-if="!isNotificationsSupported">Sorry, your browser does not support push notifications.</div>
        </div>
        <div class="p-4 fs-6">
          <div class="fw-bold">
            <span>Game Events</span>
            <a role="button" v-if="!allGameEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="showAllGameEvents">Show All</a>
            <a role="button" v-if="allGameEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="hideAllGameEvents">Hide All</a>
          </div>
          <div class="py-3 ps-4">
            <VSwitch v-model="filters.showGoals" id="showGoals" name="check-button" size="lg" class="mb-3" @change="saveFiltersToLocalStorage" switch
              >Goal</VSwitch
            >
            <VSwitch
              v-model="filters.showPenalties"
              id="showPenalties"
              name="check-button"
              size="lg"
              class="my-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Penalty</VSwitch
            >
            <VSwitch
              v-model="filters.showPeriodStarts"
              id="showPeriodStarts"
              name="check-button"
              size="lg"
              class="my-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Period Start</VSwitch
            >
            <VSwitch
              v-model="filters.showPeriodEnds"
              id="showPeriodEnds"
              name="check-button"
              size="lg"
              class="my-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Period End</VSwitch
            >
            <VSwitch
              v-model="filters.showGameEnds"
              id="showGameEnds"
              name="check-button"
              size="lg"
              class="my-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Game End</VSwitch
            >
            <VSwitch
              v-model="filters.showChallenges"
              id="showChallenges"
              name="check-button"
              size="lg"
              class="my-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Coach's Challenge</VSwitch
            >
          </div>

          <div class="fw-bold">
            <span>Lobby Events</span>
            <a role="button" v-if="!allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="showAllLobbyEvents">Show All</a>
            <a role="button" v-if="allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="hideAllLobbyEvents">Hide All</a>
          </div>
          <div class="py-3 ps-4">
            <VSwitch
              v-model="filters.showUserJoin"
              id="showUserJoin"
              name="check-button"
              size="lg"
              class="mb-3"
              @change="saveFiltersToLocalStorage"
              switch
              >User Joined</VSwitch
            >
            <VSwitch
              v-model="filters.showNameChange"
              id="showNameChange"
              name="check-button"
              size="lg"
              class="mb-3"
              @change="saveFiltersToLocalStorage"
              switch
              >Name Change</VSwitch
            >
            <VSwitch v-model="filters.showPicks" id="showPicks" name="check-button" size="lg" class="my-3" @change="saveFiltersToLocalStorage" switch
              >Pick Player</VSwitch
            >
            <VSwitch
              v-model="filters.showDrinkAwarded"
              id="showDrinkAwarded"
              name="check-button"
              size="lg"
              @change="saveFiltersToLocalStorage"
              class="my-3"
              switch
              >Drink Awarded</VSwitch
            >
            <VSwitch
              v-model="filters.showDrinkAssigned"
              id="showDrinkAssigned"
              name="check-button"
              size="lg"
              @change="saveFiltersToLocalStorage"
              class="my-3"
              switch
              >Drink Assigned</VSwitch
            >
          </div>
        </div>
      </div>

      <template v-if="currentView === 'list'">
        <div class="bg-stone-100">
          <div class="fw-bold text-uppercase text-center border py-2 bg-stone-0" style="border-bottom: none !important">Drink Timeline</div>
          <table class="w-100 border">
            <tbody>
              <tr v-for="drink in assignedDrinks" :key="drink.id" class="border">
                <td class="p-2 text-stone-500" style="width: 80px">{{ formatAsTime(drink.assigned ?? new Date()) }}</td>
                <td class="p-2 fw-bold text-right" style="width: 0">
                  <span style="white-space: pre">{{ getNameByDrink(drink) }}</span>
                </td>
                <td class="p-2" style="width: 0">
                  <div class="d-flex">
                    <span class="fs-6 d-block">🍺</span>
                    <i class="fi fi-sr-arrow-right fs-5 d-block mb-n2"></i>
                  </div>
                </td>
                <td class="p-2 fw-bold">{{ getRecipientNameByDrink(drink) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <template v-if="currentView === 'feed'">
        <VFeedItem v-for="(item, idx) in filteredItems" :key="idx" :item="item"></VFeedItem>
      </template>
    </div>
  </div>
</template>
