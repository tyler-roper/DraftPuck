<script setup lang="ts">
import { onMounted } from 'vue'

const props = withDefaults(
  defineProps<{
    /** Who gave you the drink. */
    senderName: string
    /** The whole animation (pour, hold, drain), in ms. */
    durationMs?: number
  }>(),
  { durationMs: 5000 }
)

// Bubbles rising through the beer: spread across the screen, a mix of sizes and speeds.
const bubbles = Array.from({ length: 26 }, (_, i) => ({
  left: ((i + Math.random()) / 26) * 100,
  size: 4 + Math.random() * 10,
  delay: Math.random() * 2.5,
  duration: 1.8 + Math.random() * 1.8
}))

onMounted(() => {
  // A buzz too, where the phone supports it (Android): a drink should interrupt you.
  try {
    navigator.vibrate?.([250, 120, 250])
  } catch {
    // Not allowed or not supported: the animation is enough.
  }
})
</script>

<template>
  <!-- Covers the whole app and swallows taps until it drains away. -->
  <div class="drink-pour" role="alert" aria-live="assertive" :style="{ '--pour-duration': `${props.durationMs}ms` }"
    @click.stop.prevent @touchstart.stop.prevent>
    <div class="stream"></div>

    <div class="beer">
      <div class="foam"></div>
      <div class="liquid">
        <span v-for="(b, i) in bubbles" :key="i" class="bubble"
          :style="{ left: `${b.left}%`, width: `${b.size}px`, height: `${b.size}px`, animationDelay: `${b.delay}s`, animationDuration: `${b.duration}s` }"></span>
      </div>
    </div>

    <div class="message text-center">
      <span class="d-block drink-word text-uppercase">Drink!</span>
      <span class="d-block courtesy text-uppercase">Courtesy of</span>
      <span class="d-block sender text-uppercase">{{ senderName }}</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
// The pour, as fractions of the whole animation: fill until 28%, hold, drain from 84%.
$fill-end: 28%;
$drain-start: 84%;

.drink-pour {
  position: fixed;
  inset: 0;
  z-index: 200;
  overflow: hidden;
  touch-action: none;
  background: rgba(12, 10, 9, 0.55);
  animation: backdrop var(--pour-duration) ease-in-out both;
}

// The stream of beer falling from the top while the glass fills.
.stream {
  position: absolute;
  top: 0;
  left: 50%;
  width: 34px;
  height: 100%;
  margin-left: -17px;
  border-radius: 0 0 17px 17px;
  background: linear-gradient(90deg, #e08a00, #ffc93c 40%, #fff2b0 50%, #ffc93c 60%, #e08a00);
  box-shadow: 0 0 18px rgba(255, 180, 0, 0.6);
  transform-origin: top;
  animation: stream var(--pour-duration) ease-in both;
}

// The beer: rises from the bottom, holds, then drains.
.beer {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 0;
  animation: fill var(--pour-duration) cubic-bezier(0.3, 0.6, 0.4, 1) both;
}

.liquid {
  position: absolute;
  inset: 0;
  overflow: hidden;
  // A photo of lager (Monaz Hastam, Unsplash; cropped below the surface and warmed to gold), sized to cover the
  // screen and anchored at the bottom: it stays still as the beer rises, and its densest bubbles end up at the top.
  background: url(/img/beer-pour.jpg) center bottom / max(100vw, 148vh) auto no-repeat, #f2b000;
}

// The head: a wavy line of foam that sloshes from side to side.
.foam {
  position: absolute;
  z-index: 1;
  left: -50%;
  width: 200%;
  height: 46px;
  top: -30px;
  background: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 40' preserveAspectRatio='none'%3E%3Cpath d='M0 14 Q25 2 50 14 T100 14 T150 14 T200 14 V40 H0Z' fill='%23fffaf0'/%3E%3C/svg%3E") 0 0 / 50% 100% repeat-x;
  filter: drop-shadow(0 4px 3px rgba(150, 90, 0, 0.35));
  animation: slosh 1.2s ease-in-out infinite alternate;
}

.foam::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  // Overlaps the wave's base, so no seam shows between them.
  top: 34px;
  height: 32px;
  background: linear-gradient(#fffaf0, rgba(255, 250, 240, 0));
}

.bubble {
  position: absolute;
  bottom: -20px;
  border-radius: 50%;
  background: radial-gradient(circle at 35% 35%, rgba(255, 255, 255, 0.95), rgba(255, 255, 255, 0.35) 60%, transparent 70%);
  animation-name: rise;
  animation-timing-function: ease-in;
  animation-iteration-count: infinite;
}

.message {
  position: absolute;
  inset: 0;
  z-index: 2;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: white;
  text-shadow: 0 4px 0 rgba(120, 60, 0, 0.6), 0 0 30px rgba(0, 0, 0, 0.35);
  animation: message var(--pour-duration) ease-out both;
}

.drink-word {
  font-size: clamp(72px, 26vw, 140px);
  font-weight: 900;
  line-height: 1;
  letter-spacing: 0.02em;
}

.courtesy {
  margin-top: 12px;
  font-size: 1.1rem;
  font-weight: 700;
  opacity: 0.85;
  letter-spacing: 0.15em;
}

.sender {
  font-size: clamp(28px, 9vw, 48px);
  font-weight: 800;
  line-height: 1.1;
  padding: 0 16px;
}

@keyframes backdrop {
  0% {
    opacity: 0;
  }
  6%,
  92% {
    opacity: 1;
  }
  100% {
    opacity: 0;
  }
}

@keyframes stream {
  0% {
    transform: scaleY(0);
    opacity: 1;
  }
  10% {
    transform: scaleY(1);
  }
  #{$fill-end} {
    transform: scaleY(1);
    opacity: 1;
  }
  #{$fill-end + 4%},
  100% {
    transform: scaleY(1) translateY(100%);
    opacity: 0;
  }
}

@keyframes fill {
  0%,
  6% {
    height: 0;
  }
  #{$fill-end} {
    height: calc(100% + 30px);
  }
  #{$drain-start} {
    height: calc(100% + 30px);
  }
  100% {
    height: 0;
  }
}

@keyframes slosh {
  from {
    transform: translateX(0);
  }
  to {
    transform: translateX(-12%);
  }
}

@keyframes rise {
  from {
    transform: translateY(0);
    opacity: 0;
  }
  15% {
    opacity: 1;
  }
  to {
    transform: translateY(-105vh);
    opacity: 0.6;
  }
}

@keyframes message {
  0%,
  #{$fill-end - 4%} {
    opacity: 0;
    transform: scale(0.4);
  }
  #{$fill-end + 6%} {
    opacity: 1;
    transform: scale(1.12);
  }
  #{$fill-end + 10%} {
    transform: scale(1);
  }
  #{$drain-start - 2%} {
    opacity: 1;
    transform: scale(1);
  }
  #{$drain-start + 6%},
  100% {
    opacity: 0;
    transform: scale(0.9) translateY(30px);
  }
}

@media (prefers-reduced-motion: reduce) {
  .stream,
  .bubble {
    display: none;
  }

  .beer {
    animation: none;
    height: calc(100% + 30px);
  }

  .foam,
  .message {
    animation: none;
  }
}
</style>
