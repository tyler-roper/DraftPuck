<script setup lang="ts">
import { useGiveDrinks } from '@/composables/useGiveDrinks'
import VIcon from '@/components/VIcon.vue'
import VGiveDrinkSheet from '@/components/VGiveDrinkSheet.vue'

/**
 * Your drinks to give, at the bottom of the lobby: the biggest moment in the game, so it shines. Dismissing it shrinks
 * it to a glowing chip (never away: it's how you give drinks), and a new drink brings the full bar back. It sits above
 * the bottom nav in the page's flow rather than over it, so it never covers the chat box.
 */
const { pendingDrinks, isCollapsed, isSheetOpen, collapse, openSheet } = useGiveDrinks()
</script>

<template>
  <div v-if="pendingDrinks.length > 0" class="give-drink-area">
    <Transition name="rise" mode="out-in">
      <button v-if="isCollapsed" key="chip" type="button" class="give-chip" @click="openSheet">
        <span class="beer">🍺</span> {{ pendingDrinks.length }} to give
      </button>

      <div v-else key="bar" class="give-bar">
        <button type="button" class="give-button" @click="openSheet">
          <span class="beer fs-3 me-3">🍺</span>
          <span class="d-block text-start min-w-0 flex-grow-1">
            <span class="d-block title">{{
              pendingDrinks.length === 1 ? 'You have a drink to give' : `You have ${pendingDrinks.length} drinks to give`
            }}</span>
            <span class="d-block fs-7">Tap to choose who drinks.</span>
          </span>
          <VIcon prefix="sr" icon="angle-right" class="fs-5 ms-2" />
        </button>
        <button type="button" class="dismiss" title="Later" aria-label="Shrink this until your next drink" @click="collapse">
          <VIcon prefix="sr" icon="angle-small-down" class="fs-4" />
        </button>
      </div>
    </Transition>
  </div>

  <Teleport to="body">
    <VGiveDrinkSheet v-if="isSheetOpen" />
  </Teleport>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.give-drink-area {
  display: flex;
  justify-content: center;
  padding: 8px 12px;
  background: map-get($custom-colors, 'stone-900');
}

.give-bar {
  position: relative;
  width: 100%;
  max-width: 480px;
  display: flex;
  align-items: stretch;
  border-radius: 15px;
  overflow: hidden;
  background-color: map-get($custom-colors, 'primary');
  box-shadow: 0 0 18px rgba(255, 177, 0, 0.35);
}

// A streak of light sweeps across, then rests, so the bar catches the eye without strobing.
.give-bar::after {
  content: '';
  position: absolute;
  top: 0;
  left: -60%;
  width: 45%;
  height: 100%;
  background: linear-gradient(100deg, transparent, rgba(255, 255, 255, 0.55), transparent);
  transform: skewX(-20deg);
  pointer-events: none;
  animation: shine 3.5s ease-in-out infinite;
}

@keyframes shine {
  0% {
    left: -60%;
  }
  40%,
  100% {
    left: 130%;
  }
}

.give-button,
.dismiss {
  position: relative;
  z-index: 1;
  border: none;
  background: transparent;
  color: map-get($custom-colors, 'stone-900');
}

.give-button {
  flex-grow: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  padding: 10px 8px 10px 16px;
}

.dismiss {
  padding: 0 14px;
  border-left: 1px solid rgba(0, 0, 0, 0.15);
}

.title {
  font-weight: bold;
  text-transform: uppercase;
}

.beer {
  display: inline-block;
  animation: clink 3.5s ease-in-out infinite;
}

@keyframes clink {
  0%,
  60%,
  100% {
    transform: rotate(0);
  }
  68% {
    transform: rotate(-14deg);
  }
  76% {
    transform: rotate(10deg);
  }
  84% {
    transform: rotate(-4deg);
  }
}

// Shrunk: a pill that keeps glowing until every drink is given.
.give-chip {
  padding: 6px 16px;
  border-radius: 999px;
  border: none;
  font-weight: 700;
  background-color: map-get($custom-colors, 'primary');
  color: map-get($custom-colors, 'stone-900');
  animation: glow 2.2s ease-out infinite;
}

@keyframes glow {
  0% {
    box-shadow: 0 0 0 0 rgba(255, 177, 0, 0.7);
  }
  70%,
  100% {
    box-shadow: 0 0 0 10px rgba(255, 177, 0, 0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .give-bar::after {
    display: none;
  }

  .beer,
  .give-chip {
    animation: none;
  }
}

.min-w-0 {
  min-width: 0;
}

.rise-enter-active,
.rise-leave-active {
  transition:
    transform 0.2s ease,
    opacity 0.2s ease;
}

.rise-enter-from,
.rise-leave-to {
  transform: translateY(12px);
  opacity: 0;
}
</style>
