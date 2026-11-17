<script setup lang="ts">
import LobbyEventType from '@/enums/lobbyEventType'
import FeedItem from '@/models/feedItem'
import { ref, computed } from 'vue'
import VSwitch from '@/components/VSwitch.vue'
import VFeedItem from '@/components/VFeedItem.vue'
import VDrinkTimeline from '@/components/VDrinkTimeline.vue'
import PlayType from '@/enums/playType'
import FeedItemType from '@/enums/feedItemType'
import { useNewArrivals } from '@/composables/useNewArrivals'
import { chainKeys, goalKeyFor } from '@/helpers/feedHelpers'

//const
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
  showPicks: [LobbyEventType.NewPick, LobbyEventType.BotPicksMade],
  showDrinkAwarded: LobbyEventType.DrinkAwarded,
  showDrinkAssigned: LobbyEventType.DrinkAssigned
})

//computed
const filteredItems = computed(() => {
  return props.items.filter((item, idx, array) => {
    const relevantFilterLookups = item.type === FeedItemType.GameEvent ? gameEventFilterLookup.value : lobbyEventFilterLookup.value

    // A filter covers one event type, or several (e.g. Picks covers members' and bots' picks).
    const filterLookup = Object.entries(relevantFilterLookups).find(([_key, value]) => [value].flat().includes(item.subType))
    const shouldShow = filterLookup ? filters.value[filterLookup[0]] : true

    const isDuplicate =
      array[idx + 1] && item.subType === PlayType.PeriodEnd && array[idx + 1].subType === PlayType.GameEnd && array[idx + 1].gameId === item.gameId

    return shouldShow && !isDuplicate
  })
})

// Links between a goal and its drinks: each drink item points back to its goal (when it's in the feed). Following a
// link focuses the feed on that goal: everything else fades and collapses, so the goal and its drinks sit together,
// until "Show all".
const goalsByKey = computed(() => new Map(filteredItems.value.filter((i) => i.layout === 'goal').map((i) => [i.key, i])))
const focus = ref<{ goalKey: string; keys: Set<string>; label: string }>()
const feedRoot = ref<HTMLDivElement>()

function followLink(goalKey: string) {
  if (focus.value?.goalKey === goalKey) return showAll()

  const goal = goalsByKey.value.get(goalKey)
  focus.value = { goalKey, keys: new Set(chainKeys(filteredItems.value, goalKey)), label: goal?.scorer ? `${goal.scorer}'s goal` : 'this goal' }
  feedRoot.value?.scrollTo({ top: 0, behavior: 'smooth' })
}

function showAll() {
  focus.value = undefined
}

// Only items that just arrived slide in; not the first render, a load, or items a filter brings back.
const arrivals = useNewArrivals(() => filteredItems.value.map((item) => item.key))

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
})()

function initializeFilters() {
  const existingFilters = localStorage.getItem('feedFilters')
  if (existingFilters) filters.value = { ...filters.value, ...JSON.parse(existingFilters) }
}

function saveFiltersToLocalStorage() {
  localStorage.setItem('feedFilters', JSON.stringify(filters.value))
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
</script>

<template>
  <div ref="feedRoot" style="overflow-y: scroll; background: #13100e" class="text-stone-800 d-flex flex-column">
    <div class="bg-stone-900 text-stone-0 py-1 px-3 ls-2 shadow d-flex align-items-center" style="z-index: 2; position: sticky; top: 0">
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
      <div v-if="currentView === 'settings'" class="flex-grow-1 feed-settings">
        <div class="settings-intro">Choose what shows in the feed.</div>
        <div class="fs-6">
          <div class="fw-bold group-title">
            <span>Game Events</span>
            <a role="button" v-if="!allGameEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="showAllGameEvents">Show All</a>
            <a role="button" v-if="allGameEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="hideAllGameEvents">Hide All</a>
          </div>
          <div class="group">
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

          <div class="fw-bold group-title">
            <span>Lobby Events</span>
            <a role="button" v-if="!allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="showAllLobbyEvents">Show All</a>
            <a role="button" v-if="allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ms-3" @click="hideAllLobbyEvents">Hide All</a>
          </div>
          <div class="group">
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

      <VDrinkTimeline v-if="currentView === 'list'" />

      <template v-if="currentView === 'feed'">
        <!-- Keyed by item, not index, so an item inserted mid-list doesn't shift every key after it. -->
        <TransitionGroup name="feed-slide" tag="div" class="feed-list">
          <VFeedItem
            v-for="item in filteredItems"
            :key="item.key"
            :item="item"
            :linked-goal="goalsByKey.get(goalKeyFor(item) ?? '')"
            :class="{ 'is-new': arrivals.isNew(item.key), collapsed: focus && !focus.keys.has(item.key) }"
            @follow-link="followLink"
            :style="{ '--arrival': Math.max(arrivals.arrivalOrder(item.key), 0) }"
          />
        </TransitionGroup>
        <!-- After the list in the markup: the container is a reversed column, so this shows above it. -->
        <div v-if="focus" class="focus-bar">
          <span>Showing <strong>{{ focus.label }}</strong> and its drinks</span>
          <button type="button" @click="showAll">✕ Show all</button>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
/* New arrivals slide down into place at the top (newest first, 80ms apart), and the items below glide down to make
   room. Nothing animates when items leave. */
.feed-slide-enter-active.is-new {
  transition:
    opacity 0.35s ease,
    transform 0.35s ease;
  transition-delay: calc(var(--arrival) * 80ms);
}

.feed-slide-enter-from.is-new {
  opacity: 0;
  transform: translateY(-100%);
}

.feed-slide-move {
  transition: transform 0.35s ease;
}

/* Feed settings: dark, each group of switches a card. */
.feed-settings {
  padding: 12px 10px 20px;
  color: #e7e5e4;
  background: #13100e;
}

.settings-intro {
  margin: 2px 4px 12px;
  font-size: 13px;
  color: #a8a29e;
}

.group-title {
  display: flex;
  align-items: baseline;
  margin: 4px 4px 8px;
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: #a8a29e;
}

.group {
  margin-bottom: 16px;
  padding: 6px 14px;
  border-radius: 12px;
  background: #1e1b19;
}

.group :deep(.form-check) {
  margin: 0 !important;
  padding-top: 10px;
  padding-bottom: 10px;
  border-bottom: 1px solid #2b2623;
}

.group :deep(.form-check:last-child) {
  border-bottom: 0;
}

.group :deep(.form-check-label) {
  color: #f5f5f4;
}

/* The items, newest at the top (the column is reversed), as cards on a dark background. */
.feed-list {
  flex-grow: 1;
  display: flex;
  flex-direction: column-reverse;
  justify-content: flex-end;
  padding: 6px 10px;
  background: #13100e;
}

/* Spacing as margins (not gap), so a collapsed item leaves no space behind. */
.feed-list > * {
  margin: 4px 0;
  max-height: 600px;
  transition:
    opacity 0.3s ease,
    max-height 0.35s ease,
    margin 0.35s ease,
    min-height 0.35s ease;
}

/* Focused on a goal: everything else fades and collapses, so the goal and its drinks sit together. */
.feed-list > .collapsed {
  opacity: 0;
  max-height: 0;
  min-height: 0;
  margin: 0;
  border-width: 0;
  pointer-events: none;
}

.focus-bar {
  position: sticky;
  top: 52px;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin: 8px 10px 0;
  padding: 8px 12px;
  border-radius: 999px;
  font-size: 13px;
  color: #fde68a;
  background: #2b2623;
  border: 1px solid #ffb100;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.5);
}

.focus-bar strong {
  color: #fff;
}

.focus-bar button {
  border: 0;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 800;
  color: #1c1917;
  background: #ffb100;
}

@media (prefers-reduced-motion: reduce) {
  .feed-slide-enter-active.is-new,
  .feed-slide-move {
    transition: none;
  }
}
</style>
