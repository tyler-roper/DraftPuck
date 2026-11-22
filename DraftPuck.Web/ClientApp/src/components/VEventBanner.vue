<script setup lang="ts">
import { computed } from 'vue'
import { bannerStyle, rainDrops } from '@/helpers/calloutHelpers'
import VIcon from '@/components/VIcon.vue'

const props = withDefaults(
  defineProps<{
    /** A callout or a drink, its text already filled in. */
    event: LobbyEvent
    /** How long the banner shows, in ms (the whole animation, in and out). */
    durationMs?: number
  }>(),
  { durationMs: 4500 }
)

const style = computed(() => bannerStyle(props.event.lobbyEventType) ?? { icon: 'star', color: '#ffb100', motion: 'flicker', emoji: '⭐' })
// Made once per banner (each callout gets a new one), so the drops don't reshuffle mid-fall.
const drops = rainDrops()
</script>

<template>
  <!-- The event's emoji rains down the screen behind the banner. Decorative, so hidden from screen readers. -->
  <div class="callout-rain" aria-hidden="true">
    <span
      v-for="(drop, i) in drops"
      :key="i"
      class="drop"
      :style="{
        left: `${drop.left}%`,
        fontSize: `${drop.size}px`,
        animationDelay: `${drop.delayMs}ms`,
        animationDuration: `${drop.durationMs}ms`,
        '--drift': `${drop.drift}px`,
        '--spin': `${drop.spin}deg`
      }"
      >{{ style.emoji }}</span
    >
  </div>

  <!-- The event's text has been filled in from the lobby (names are HTML-escaped). -->
  <div class="callout" role="status" aria-live="polite" :style="{ '--callout-color': style.color, '--callout-duration': `${durationMs}ms` }">
    <div class="callout-icon" :class="`motion-${style.motion}`">
      <VIcon :icon="style.icon" prefix="sr" />
    </div>
    <div class="flex-grow-1 text-start">
      <span class="d-block callout-title text-uppercase fw-bold">{{ event.title }}</span>
      <span class="d-block callout-text" v-html="event.text"></span>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.callout {
  position: fixed;
  z-index: 100;
  top: 12px;
  left: 50%;
  width: min(440px, calc(100% - 24px));
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
  overflow: hidden;
  color: white;
  border-radius: 16px;
  border: 2px solid var(--callout-color);
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--callout-color) 35%, map-get($custom-colors, 'stone-900')),
    map-get($custom-colors, 'stone-900') 70%
  );
  box-shadow:
    0 0 28px color-mix(in srgb, var(--callout-color) 55%, transparent),
    0 10px 30px black;
  animation: callout-in-out var(--callout-duration) ease-in-out both;
}

// A shine that sweeps across once it has landed.
.callout::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(100deg, transparent 30%, rgba(255, 255, 255, 0.25) 50%, transparent 70%);
  transform: translateX(-120%);
  animation: callout-shine 1.1s 0.5s ease-out both;
  pointer-events: none;
}

.callout-icon {
  flex: none;
  display: grid;
  place-items: center;
  width: 58px;
  height: 58px;
  border-radius: 50%;
  font-size: 30px;
  color: var(--callout-color);
  background: color-mix(in srgb, var(--callout-color) 18%, map-get($custom-colors, 'stone-900'));
  box-shadow: 0 0 18px color-mix(in srgb, var(--callout-color) 60%, transparent);
}

.callout-title {
  font-size: 1.35rem;
  line-height: 1.1;
  letter-spacing: 0.06em;
  color: var(--callout-color);
}

.callout-text {
  font-size: 0.95rem;
  color: map-get($custom-colors, 'stone-200');
}

.callout-text :deep(strong) {
  color: white;
}

.callout-rain {
  position: fixed;
  inset: 0;
  z-index: 99;
  overflow: hidden;
  pointer-events: none;
}

.drop {
  position: absolute;
  top: -60px;
  line-height: 1;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.5));
  animation-name: fall;
  animation-timing-function: cubic-bezier(0.35, 0, 0.75, 1);
  animation-fill-mode: both;
}

@keyframes fall {
  0% {
    opacity: 0;
    transform: translate(0, 0) rotate(0);
  }
  8% {
    opacity: 1;
  }
  85% {
    opacity: 1;
  }
  100% {
    opacity: 0;
    transform: translate(var(--drift), calc(100vh + 80px)) rotate(var(--spin));
  }
}

.motion-flicker {
  animation: flicker 0.6s ease-in-out infinite alternate;
}

.motion-wobble {
  animation: wobble 0.9s ease-in-out infinite;
}

.motion-clash {
  animation: clash 0.7s ease-in-out infinite;
}

.motion-shiver {
  animation: shiver 0.25s linear infinite;
}

@keyframes callout-in-out {
  0% {
    opacity: 0;
    transform: translate(-50%, -140%) scale(0.9);
  }
  9% {
    opacity: 1;
    transform: translate(-50%, 10px) scale(1.03);
  }
  14% {
    transform: translate(-50%, 0) scale(1);
  }
  88% {
    opacity: 1;
    transform: translate(-50%, 0) scale(1);
  }
  100% {
    opacity: 0;
    transform: translate(-50%, -140%) scale(0.95);
  }
}

@keyframes callout-shine {
  to {
    transform: translateX(120%);
  }
}

@keyframes flicker {
  from {
    transform: scale(1) rotate(-4deg);
    filter: brightness(1);
  }
  to {
    transform: scale(1.15) rotate(4deg);
    filter: brightness(1.35);
  }
}

@keyframes wobble {
  0%,
  100% {
    transform: rotate(0);
  }
  25% {
    transform: rotate(-14deg);
  }
  75% {
    transform: rotate(14deg);
  }
}

@keyframes clash {
  0%,
  100% {
    transform: rotate(0) scale(1);
  }
  40% {
    transform: rotate(-25deg) scale(1.1);
  }
  55% {
    transform: rotate(10deg) scale(1.15);
  }
}

@keyframes shiver {
  0%,
  100% {
    transform: translateX(0);
  }
  25% {
    transform: translateX(-2px) rotate(-3deg);
  }
  75% {
    transform: translateX(2px) rotate(3deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .callout-rain {
    display: none;
  }

  .callout,
  .callout::after,
  .callout-icon {
    animation: none;
  }

  .callout {
    transform: translate(-50%, 0);
  }
}
</style>
