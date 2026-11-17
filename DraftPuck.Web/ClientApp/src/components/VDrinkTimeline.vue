<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import { useLobbyStore } from '@/stores/lobby'
import { useGamesStore } from '@/stores/games'
import { drinkTallies, drinkTimeline } from '@/helpers/timelineHelpers'

/** The night's drinks at a glance: who's given and taken how many, then every drink handed out, newest first. */
const { lobby } = storeToRefs(useLobbyStore())
const { games } = storeToRefs(useGamesStore())

const drinks = computed(() => (lobby.value ? drinkTimeline(lobby.value, games.value) : []))
const tallies = computed(() => drinkTallies(drinks.value))
const most = computed(() => Math.max(1, ...tallies.value.flatMap((t) => [t.given, t.taken])))

const avatarStyle = (member: LobbyMember) => (member.avatarPath && !member.isGuest ? { backgroundImage: `url(${member.avatarPath})` } : {})
const initial = (member: LobbyMember) => (member.avatarPath && !member.isGuest ? '' : member.name.charAt(0).toUpperCase())
</script>

<template>
  <div class="timeline">
    <div v-if="!drinks.length" class="empty">
      <span class="d-block fs-1">🍺</span>
      No drinks handed out yet.
    </div>

    <template v-else>
      <section class="tally">
        <div class="section-title">Tonight · {{ drinks.length }} {{ drinks.length === 1 ? 'drink' : 'drinks' }} handed out</div>
        <div v-for="t in tallies" :key="t.member.id" class="tally-row">
          <span class="avatar" :style="avatarStyle(t.member)">{{ initial(t.member) }}</span>
          <span class="tally-name text-truncate">{{ t.member.name }}</span>
          <span class="bars">
            <span class="bar given" :style="{ width: `${(t.given / most) * 100}%` }"></span>
            <span class="bar taken" :style="{ width: `${(t.taken / most) * 100}%` }"></span>
          </span>
          <span class="counts"><span class="given-count">{{ t.given }}</span> · <span class="taken-count">{{ t.taken }}</span></span>
        </div>
        <div class="legend"><span class="given-count">■ given</span><span class="taken-count">■ taken</span></div>
      </section>

      <ol class="drinks">
        <li v-for="drink in drinks" :key="drink.id" class="drink">
          <span class="time">{{ format(drink.time, 'p') }}</span>
          <span class="dot"></span>
          <div class="card">
            <div class="who">
              <span class="avatar" :style="avatarStyle(drink.giver)">{{ initial(drink.giver) }}</span>
              <span class="arrow">🍺→</span>
              <span class="avatar" :style="avatarStyle(drink.recipient)">{{ initial(drink.recipient) }}</span>
              <span class="names text-truncate"><strong>{{ drink.giver.name }}</strong> → <strong>{{ drink.recipient.name }}</strong></span>
            </div>
            <div v-if="drink.player || drink.message" class="extra">
              <span v-if="drink.player" class="player" :style="{ background: drink.player.color ?? '#57534e' }">{{ drink.player.abbreviation }} · {{ drink.player.lastName }}</span>
              <span v-if="drink.message" class="message">“{{ drink.message }}”</span>
            </div>
          </div>
        </li>
      </ol>
    </template>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

$amber: map-get($custom-colors, 'primary');
$red: #f87171;

.timeline {
  flex-grow: 1;
  padding: 10px;
  color: map-get($custom-colors, 'stone-200');
  background: #13100e;
}

.empty {
  padding: 48px 16px;
  text-align: center;
  font-weight: 700;
  color: map-get($custom-colors, 'stone-400');
}

.section-title {
  margin-bottom: 8px;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: map-get($custom-colors, 'stone-400');
}

.avatar {
  flex: none;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  line-height: 1;
  font-size: 12px;
  font-weight: 800;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;
}

// Who's given and taken how many: amber and red bars, scaled to the night's busiest.
.tally {
  padding: 12px;
  border-radius: 12px;
  background: map-get($custom-colors, 'stone-850');
}

.tally-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 4px 0;
}

.tally-name {
  width: 90px;
  flex: none;
  font-weight: 700;
  color: white;
}

.bars {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.bar {
  display: block;
  height: 6px;
  min-width: 2px;
  border-radius: 3px;
  transition: width 0.4s ease;
}

.bar.given {
  background: $amber;
}

.bar.taken {
  background: $red;
}

.counts {
  width: 44px;
  flex: none;
  text-align: right;
  font-size: 12px;
  font-weight: 800;
  color: map-get($custom-colors, 'stone-500');
}

.given-count {
  color: $amber;
}

.taken-count {
  color: $red;
}

.legend {
  display: flex;
  gap: 14px;
  margin-top: 6px;
  font-size: 11px;
  font-weight: 700;
}

// Every drink, newest first, down a line with the time beside it.
.drinks {
  list-style: none;
  margin: 12px 0 0;
  padding: 0;
  position: relative;
}

.drinks::before {
  content: '';
  position: absolute;
  left: 64px;
  top: 6px;
  bottom: 6px;
  width: 2px;
  background: map-get($custom-colors, 'stone-700');
}

.drink {
  position: relative;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 5px 0;
}

.time {
  width: 52px;
  flex: none;
  text-align: right;
  font-size: 11px;
  color: map-get($custom-colors, 'stone-400');
}

.dot {
  z-index: 1;
  width: 10px;
  height: 10px;
  flex: none;
  margin-left: 2px;
  border-radius: 50%;
  background: $amber;
  box-shadow: 0 0 0 3px #13100e;
}

.card {
  flex: 1;
  min-width: 0;
  padding: 8px 10px;
  border-radius: 10px;
  background: linear-gradient(90deg, color-mix(in srgb, $amber 12%, map-get($custom-colors, 'stone-850')), map-get($custom-colors, 'stone-850') 70%);
}

.who {
  display: flex;
  align-items: center;
  gap: 6px;
}

.arrow {
  flex: none;
  white-space: nowrap;
  font-size: 12px;
  font-weight: 900;
  color: $amber;
}

.names {
  margin-left: 4px;
  font-size: 13.5px;
}

.names strong {
  color: white;
}

.extra {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
}

.player {
  padding: 0 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
  color: white;
}

.message {
  font-size: 12.5px;
  font-style: italic;
  color: #fde68a;
}
</style>
