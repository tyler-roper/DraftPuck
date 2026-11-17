<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import type FeedItem from '@/models/feedItem'
import { useLobbyStore } from '@/stores/lobby'
import { drinkAvatarMemberId, lobbyEventStyle } from '@/helpers/feedItemStyles'
import { bannerStyle } from '@/helpers/calloutHelpers'
import VIcon from '@/components/VIcon.vue'
import { goalDrinks } from '@/helpers/feedHelpers'

const props = defineProps<{
  item: FeedItem
  /** For a drink event: the goal it came from, if it's in the feed. */
  linkedGoal?: FeedItem
  /** Lit up after following a link: the goal and its drinks. */
  highlighted?: boolean
}>()
const emit = defineEmits<{ followLink: [goalKey: string] }>()

const { lobby } = storeToRefs(useLobbyStore())

const eventStyle = computed(() => (props.item.lobbyEvent ? lobbyEventStyle(props.item.lobbyEvent.lobbyEventType) : undefined))
const callout = computed(() => (eventStyle.value?.tone === 'callout' ? bannerStyle(props.item.lobbyEvent!.lobbyEventType) : undefined))

/** The person a drink event shows: who earned the drink, or who's drinking it. */
const avatarMember = computed(() => {
  const event = props.item.lobbyEvent
  const avatar = eventStyle.value?.avatar
  if (!event || !avatar) return undefined
  const memberId = drinkAvatarMemberId(event, avatar)
  return lobby.value?.members.find((m) => m.id === memberId)
})

/** A final's drinks: every drink the lobby's picks earned in that game. */
const drinksThisGame = computed(() => {
  if (props.item.layout !== 'final' || !lobby.value) return 0
  return lobby.value.members
    .flatMap((m) => m.picks)
    .filter((p) => p.gameId === props.item.gameId && p.isActive)
    .reduce((count, p) => count + p.drinks.length, 0)
})

/** A goal's drinks: who earned each, and who it went to (filled in as they're handed out). */
const trail = computed(() =>
  props.item.layout === 'goal' && lobby.value && props.item.gameId != null && props.item.playId != null
    ? goalDrinks(lobby.value, props.item.gameId, props.item.playId)
    : []
)

const title = computed(() => (callout.value ? `${callout.value.emoji} ${props.item.title}` : props.item.title))
const time = computed(() => format(props.item.time, 'p'))
const team = computed(() => props.item.teams[0])
const itemStyle = computed(() => ({
  '--team': team.value?.color ?? '#57534e',
  ...(callout.value ? { '--tone': callout.value.color } : {})
}))
</script>

<template>
  <div class="feed-item" :class="[`layout-${item.layout}`, eventStyle && `tone-${eventStyle.tone}`, { highlighted }]" :style="itemStyle" :data-feed-key="item.key">
    <div class="side">
      <img v-if="item.teams.length === 1" class="logo" :src="`/img/logos/${team.logo}`" :alt="team.abbreviation" />
      <span v-else-if="item.teams.length === 2" class="pair">
        <img v-for="t in item.teams" :key="t.abbreviation" :src="`/img/logos/${t.logo}`" :alt="t.abbreviation" />
      </span>
      <span v-else-if="eventStyle?.avatar" class="person" :class="eventStyle.avatar">
        <span class="avatar" :style="avatarMember?.avatarPath && !avatarMember.isGuest ? { backgroundImage: `url(${avatarMember.avatarPath})` } : {}">{{
          avatarMember?.avatarPath && !avatarMember.isGuest ? '' : (avatarMember?.name.charAt(0).toUpperCase() ?? '?')
        }}</span>
        <span class="badge-corner">{{ eventStyle.avatar === 'earner' ? '🚨' : '🍺' }}</span>
      </span>
      <span v-else-if="eventStyle" class="ico"><VIcon :icon="eventStyle.icon" prefix="sr" /></span>
    </div>

    <div class="main">
      <div class="head">
        <span class="title">{{ title }}</span>
        <span class="time">{{ time }}</span>
      </div>
      <!-- Event and play text is HTML built from escaped names (and the player badge). -->
      <div class="text" v-html="item.text"></div>
      <div v-if="item.detail || drinksThisGame" class="detail">
        <span v-if="item.detail" v-html="item.detail"></span>
        <span v-if="item.detail && drinksThisGame"> · </span>
        <span v-if="drinksThisGame">🍺 {{ drinksThisGame }} {{ drinksThisGame === 1 ? 'drink' : 'drinks' }} this game</span>
      </div>
      <div v-if="trail.length" class="trail">
        <span v-for="(drink, i) in trail" :key="i" :class="drink.recipient ? 'done' : 'waiting'">
          <template v-if="drink.recipient">🍺 {{ drink.earner }} earned a drink → handed to {{ drink.recipient }}</template>
          <template v-else>🍺 {{ drink.earner }}'s drink · not handed out yet 👀</template>
        </span>
      </div>
      <button v-if="linkedGoal" type="button" class="goal-link" @click="emit('followLink', linkedGoal.key)">
        ↩ {{ linkedGoal.scorer ? `${linkedGoal.scorer}'s goal` : 'The goal' }} · {{ format(linkedGoal.time, 'p') }}
      </button>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Every item: a 64px column on the left (team block, logos, icon or avatar), then the title line, the text and a detail.
.feed-item {
  display: flex;
  min-height: 64px;
  border-radius: 12px;
  overflow: hidden;
  color: map-get($custom-colors, 'stone-200');
  background: map-get($custom-colors, 'stone-850');
}

.side {
  width: 64px;
  flex: none;
  display: flex;
  align-items: center;
  justify-content: center;
}

.main {
  flex: 1;
  min-width: 0;
  padding: 9px 12px 10px;
}

.head {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.title {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--tone, map-get($custom-colors, 'stone-400'));
}

.time {
  margin-left: auto;
  font-size: 11px;
  white-space: nowrap;
  color: map-get($custom-colors, 'stone-400');
}

.text {
  margin-top: 2px;
  font-size: 14px;
  line-height: 1.35;
  overflow-wrap: anywhere;
}

.text :deep(strong) {
  color: white;
}

.detail {
  margin-top: 3px;
  font-size: 12px;
  color: map-get($custom-colors, 'stone-400');
}

// Logos: a light outline keeps a logo visible on its own team's colour (Toronto's navy on navy).
.logo {
  width: 44px;
  filter: drop-shadow(0 0 1px rgba(255, 255, 255, 0.7)) drop-shadow(0 2px 4px rgba(0, 0, 0, 0.45));
}

.pair {
  position: relative;
  width: 48px;
  height: 48px;
}

.pair img {
  position: absolute;
  width: 32px;
  filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.5));
}

.pair img:first-child {
  left: 0;
  top: 0;
}

.pair img:last-child {
  right: 0;
  bottom: 0;
}

// Goals: the loudest thing in the feed, a solid block of the team's colour fading across the card.
.layout-goal .side {
  background: var(--team);
}

.layout-goal {
  background: linear-gradient(90deg, color-mix(in srgb, var(--team) 30%, map-get($custom-colors, 'stone-850')), map-get($custom-colors, 'stone-850') 75%);
}

.layout-goal .title {
  color: white;
}

.layout-goal .text {
  font-size: 15px;
}

// Penalties, challenges, periods and finals: quiet, on a neutral block.
.layout-game .side,
.layout-state .side,
.layout-final .side {
  background: map-get($custom-colors, 'stone-800');
}

.layout-game .logo {
  width: 32px;
}

.layout-final .title {
  color: map-get($custom-colors, 'primary');
}

.layout-final .text {
  font-size: 22px;
  font-weight: 900;
  line-height: 1.15;
  color: white;
}

.layout-final .text :deep(.loser) {
  color: map-get($custom-colors, 'stone-500');
}

// Lobby events: a round icon in the event's colour.
.tone-drink {
  --tone: #{map-get($custom-colors, 'primary')};
}

.tone-pick {
  --tone: #60a5fa;
}

.tone-social {
  --tone: #{map-get($custom-colors, 'stone-400')};
}

.tone-correction {
  --tone: #f87171;
}

.ico {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 17px;
  color: var(--tone);
  background: color-mix(in srgb, var(--tone) 18%, map-get($custom-colors, 'stone-900'));
}

// The icon font's glyphs sit low in their line box: make the glyph itself the box, so flex centres it.
.ico :deep(i) {
  display: block;
  line-height: 1;
  height: 1em;
}

.ico :deep(i::before) {
  display: block;
  line-height: 1;
  transform: none;
}

// Drinks show a person: who earned it (gold ring, goal light) or who's drinking it (beer).
.person {
  position: relative;
  width: 38px;
  height: 38px;
}

.avatar {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  line-height: 1;
  font-weight: 800;
  font-size: 15px;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;
}

.person.earner .avatar {
  box-shadow:
    0 0 0 2px map-get($custom-colors, 'primary'),
    0 0 12px rgba(255, 177, 0, 0.45);
}

.badge-corner {
  position: absolute;
  right: -5px;
  bottom: -4px;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 11px;
  background: map-get($custom-colors, 'primary');
  box-shadow: 0 0 0 2px map-get($custom-colors, 'stone-850');
}

.person.earner .badge-corner {
  background: #dc2626;
}

.tone-drink.feed-item:has(.recipient) {
  background: linear-gradient(90deg, color-mix(in srgb, map-get($custom-colors, 'primary') 14%, map-get($custom-colors, 'stone-850')), map-get($custom-colors, 'stone-850') 70%);
}

// A drink message (quoted by the server under the usual line): set apart in amber.
.tone-drink .text :deep(.fst-italic) {
  margin-top: 4px;
  padding-left: 8px;
  border-left: 2px solid map-get($custom-colors, 'primary');
  color: #fde68a;
  font-size: 13px;
}

// A goal's trail: where its drinks went.
.trail {
  margin-top: 7px;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.trail span {
  width: fit-content;
  max-width: 100%;
  padding: 2px 8px;
  border-radius: 6px;
  font-size: 12.5px;
  background: rgba(0, 0, 0, 0.28);
}

.trail .done {
  color: #fde68a;
}

.trail .waiting {
  color: map-get($custom-colors, 'stone-400');
  font-style: italic;
}

// A drink's link back to its goal.
.goal-link {
  margin-top: 6px;
  padding: 2px 9px;
  border-radius: 999px;
  font-size: 11.5px;
  font-weight: 700;
  color: map-get($custom-colors, 'stone-200');
  background: map-get($custom-colors, 'stone-800');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

// After following a link, the goal and its drinks light up together for a moment.
.feed-item {
  transition: box-shadow 0.3s ease;
}

.feed-item.highlighted {
  outline: 2px solid map-get($custom-colors, 'primary');
  outline-offset: 2px;
  box-shadow: 0 0 18px rgba(255, 177, 0, 0.4);
}

// Callouts: the flare.
.tone-callout {
  border: 1px solid var(--tone);
  box-shadow: 0 0 18px color-mix(in srgb, var(--tone) 28%, transparent);
  background: linear-gradient(110deg, color-mix(in srgb, var(--tone) 26%, map-get($custom-colors, 'stone-850')), map-get($custom-colors, 'stone-850') 70%);
}
</style>
