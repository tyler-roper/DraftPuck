<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import { useLobbyStore } from '@/stores/lobby'
import { useGamesStore } from '@/stores/games'
import { drinkTimeline } from '@/helpers/timelineHelpers'

/** Every drink handed out, newest first, down a line: who gave it to whom, for whose goal, and what they said. */
const { lobby } = storeToRefs(useLobbyStore())
const { games } = storeToRefs(useGamesStore())

const drinks = computed(() => (lobby.value ? drinkTimeline(lobby.value, games.value) : []))
</script>

<template>
  <div class="timeline">
    <div v-if="!drinks.length" class="empty">
      <span class="d-block fs-1">🍺</span>
      No drinks handed out yet.
    </div>

    <ol v-else class="drinks">
      <li v-for="drink in drinks" :key="drink.id" class="drink">
        <span class="time">{{ format(drink.time, 'p') }}</span>
        <span class="dot"></span>
        <div class="card" :style="{ '--team': drink.player?.color ?? '#57534e' }">
          <div class="names">
            <strong>{{ drink.giver.name }}</strong><span class="arrow">→</span><strong>{{ drink.recipient.name }}</strong>
          </div>
          <div v-if="drink.player" class="source">{{ drink.player.lastName }}'s goal · {{ drink.player.abbreviation }}</div>
          <div v-if="drink.message" class="message">“{{ drink.message }}”</div>
        </div>
      </li>
    </ol>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

$amber: map-get($custom-colors, 'primary');
// The line runs through the middle of the dots: time column (52px) + gap (10px) + half a dot (5px), less half the line.
$time-width: 52px;
$gap: 10px;
$dot: 10px;
$line: 2px;

.timeline {
  flex-grow: 1;
  padding: 12px 10px;
  color: map-get($custom-colors, 'stone-200');
  background: #13100e;
}

.empty {
  padding: 48px 16px;
  text-align: center;
  font-weight: 700;
  color: map-get($custom-colors, 'stone-400');
}

.drinks {
  list-style: none;
  margin: 0;
  padding: 0;
  position: relative;
}

.drinks::before {
  content: '';
  position: absolute;
  left: $time-width + $gap + calc($dot / 2) - calc($line / 2);
  top: 8px;
  bottom: 8px;
  width: $line;
  background: map-get($custom-colors, 'stone-700');
}

.drink {
  display: flex;
  align-items: center;
  gap: $gap;
  padding: 4px 0;
}

.time {
  width: $time-width;
  flex: none;
  text-align: right;
  font-size: 11px;
  color: map-get($custom-colors, 'stone-400');
}

.dot {
  position: relative;
  z-index: 1;
  width: $dot;
  height: $dot;
  flex: none;
  border-radius: 50%;
  background: $amber;
  box-shadow: 0 0 0 3px #13100e;
}

// One card per drink: who → whom, then what earned it, then what they said.
.card {
  flex: 1;
  min-width: 0;
  padding: 9px 12px;
  border-radius: 10px;
  background: map-get($custom-colors, 'stone-850');
}

.names {
  font-size: 15px;
  line-height: 1.3;
  overflow-wrap: anywhere;
}

.names strong {
  color: white;
}

.arrow {
  margin: 0 7px;
  font-weight: 900;
  color: $amber;
}

.source {
  margin-top: 2px;
  font-size: 12px;
  color: map-get($custom-colors, 'stone-400');
}

// A thin mark in the scoring team's colour.
.source::before {
  content: '';
  display: inline-block;
  width: 8px;
  height: 8px;
  margin-right: 6px;
  border-radius: 2px;
  background: var(--team);
  box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.25);
}

.message {
  margin-top: 5px;
  padding-left: 8px;
  border-left: 2px solid $amber;
  font-size: 13px;
  font-style: italic;
  color: #fde68a;
}
</style>
