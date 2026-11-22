<script setup lang="ts">
import VIcon from '@/components/VIcon.vue'
import VRecapAvatar from '@/components/VRecapAvatar.vue'
import { awardStyle } from '@/helpers/recapHelpers'

/** The post-game page's Awards tab: each award in its colour, sliding in one after another. */
const props = defineProps<{
  awards: LobbyAward[]
  isLoading: boolean
  lobby: Lobby
}>()

/** The avatars beside an award's winner: theirs, and their rival's for a rivalry. */
function winners(award: LobbyAward) {
  return [award.memberId, award.otherMemberId]
    .map((id) => props.lobby.members.find((m) => m.id === id))
    .filter((m): m is LobbyMember => m !== undefined)
}
</script>

<template>
  <div>
    <!-- Placeholders hold the awards' place while they load, so the page doesn't jump when they arrive. -->
    <template v-if="isLoading">
      <div v-for="n in 3" :key="n" class="award award-placeholder mb-2" aria-hidden="true"></div>
    </template>
    <!-- The top award keeps shining; each icon moves in its own way (see awardStyle). -->
    <div
      v-for="(award, index) in awards"
      :key="award.key"
      class="award award-in d-flex align-items-center mb-2 py-2 pe-2 ps-3"
      :class="{ top: index === 0 }"
      :style="{ '--award-color': awardStyle(award.key).color, '--delay': `${index * 90}ms` }"
    >
      <VIcon :icon="awardStyle(award.key).icon" prefix="sr" class="award-icon fs-3 me-3" :class="`motion-${awardStyle(award.key).motion}`" />
      <div class="flex-grow-1">
        <span class="d-block fs-9 fw-bold text-uppercase ls-2 award-title">{{ award.title }}</span>
        <span class="winner fw-bold">
          <span class="winner-avatars">
            <VRecapAvatar v-for="m in winners(award)" :key="m.id" :member="m" class="winner-avatar" />
          </span>
          {{ award.memberName }}
        </span>
        <span class="d-block fs-7 text-stone-300">{{ award.detail }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Each award in its colour: a gradient tint, a tinted border and a glowing icon.
.award {
  position: relative;
  overflow: hidden;
  border-radius: 10px;
  border: 1px solid color-mix(in srgb, var(--award-color) 35%, map-get($custom-colors, 'stone-700'));
  border-left: 4px solid var(--award-color);
  background: linear-gradient(
    110deg,
    color-mix(in srgb, var(--award-color) 20%, map-get($custom-colors, 'stone-850')),
    map-get($custom-colors, 'stone-850') 70%
  );
}

.award-icon,
.award-title {
  color: var(--award-color);
}

.award-icon {
  display: inline-block;
  filter: drop-shadow(0 0 6px color-mix(in srgb, var(--award-color) 70%, transparent));
}

// The same height as an award card, with a slow shimmer.
.award-placeholder {
  height: 72px;
  --award-color: #{map-get($custom-colors, 'stone-700')};
  background: linear-gradient(
    90deg,
    map-get($custom-colors, 'stone-850') 25%,
    map-get($custom-colors, 'stone-800') 50%,
    map-get($custom-colors, 'stone-850') 75%
  );
  background-size: 200% 100%;
  animation: award-shimmer 1.4s ease-in-out infinite;
}

// Cards slide in one after another, each with a shine sweeping across.
.award-in {
  animation: award-in 0.4s ease-out both;
  animation-delay: var(--delay);
}

.award-in::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: linear-gradient(100deg, transparent 30%, rgba(255, 255, 255, 0.22) 50%, transparent 70%);
  transform: translateX(-120%);
  animation: award-shine 0.9s ease-out both;
  animation-delay: calc(var(--delay) + 0.25s);
}

// The top award keeps shining, and glows.
.award.top {
  animation:
    award-in 0.4s ease-out both,
    award-glow 2.6s ease-in-out 0.6s infinite;
}

.award.top::after {
  animation: award-shine-repeat 4s ease-in-out infinite;
  animation-delay: calc(var(--delay) + 0.25s);
}

.motion-bob {
  animation: icon-bob 2.2s ease-in-out infinite;
}

.motion-shiver {
  animation: icon-shiver 0.35s linear infinite;
}

.motion-beat {
  animation: icon-beat 1.1s ease-in-out infinite;
}

.motion-jab {
  animation: icon-jab 1.6s ease-in-out infinite;
}

.motion-clash {
  animation: icon-clash 1.4s ease-in-out infinite;
}

.motion-swing {
  transform-origin: top center;
  animation: icon-swing 2s ease-in-out infinite;
}

.motion-wobble {
  animation: icon-wobble 1.8s ease-in-out infinite;
}

.motion-tick {
  animation: icon-tick 1s steps(2) infinite;
}

.motion-twinkle {
  animation: icon-twinkle 2.4s ease-in-out infinite;
}

.motion-bounce {
  animation: icon-bounce 1.3s ease-in-out infinite;
}

@keyframes award-shimmer {
  from {
    background-position: 200% 0;
  }
  to {
    background-position: -200% 0;
  }
}

@keyframes award-in {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

@keyframes award-shine {
  to {
    transform: translateX(120%);
  }
}

@keyframes award-shine-repeat {
  0% {
    transform: translateX(-120%);
  }
  30%,
  100% {
    transform: translateX(120%);
  }
}

@keyframes award-glow {
  0%,
  100% {
    box-shadow: 0 0 0 transparent;
  }
  50% {
    box-shadow: 0 0 18px color-mix(in srgb, var(--award-color) 45%, transparent);
  }
}

@keyframes icon-bob {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-3px);
  }
}

@keyframes icon-shiver {
  0%,
  100% {
    transform: translateX(0);
  }
  25% {
    transform: translateX(-1px) rotate(-4deg);
  }
  75% {
    transform: translateX(1px) rotate(4deg);
  }
}

@keyframes icon-beat {
  0%,
  100% {
    transform: scale(1);
  }
  15% {
    transform: scale(1.18);
  }
  30% {
    transform: scale(1);
  }
  45% {
    transform: scale(1.12);
  }
}

@keyframes icon-jab {
  0%,
  60%,
  100% {
    transform: translateX(0);
  }
  70% {
    transform: translateX(5px) scale(1.1);
  }
}

@keyframes icon-clash {
  0%,
  100% {
    transform: rotate(0);
  }
  40% {
    transform: rotate(-20deg);
  }
  55% {
    transform: rotate(10deg) scale(1.1);
  }
}

@keyframes icon-swing {
  0%,
  100% {
    transform: rotate(-10deg);
  }
  50% {
    transform: rotate(10deg);
  }
}

@keyframes icon-wobble {
  0%,
  100% {
    transform: rotate(0);
  }
  25% {
    transform: rotate(-10deg);
  }
  75% {
    transform: rotate(10deg);
  }
}

@keyframes icon-tick {
  from {
    transform: rotate(-6deg);
  }
  to {
    transform: rotate(6deg);
  }
}

@keyframes icon-twinkle {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(0.9);
  }
}

@keyframes icon-bounce {
  0%,
  100% {
    transform: translateY(0);
  }
  30% {
    transform: translateY(-4px);
  }
  50% {
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .award-placeholder,
  .award-in,
  .award-in::after,
  .award.top,
  .award-icon {
    animation: none !important;
  }
}

// The winner: a small avatar (two, overlapping, for a rivalry) beside the name.
.winner {
  display: flex;
  align-items: center;
  gap: 7px;
}

.winner-avatars {
  display: flex;
  flex: none;
}

.winner-avatar {
  width: 22px;
  height: 22px;
  font-size: 10px;
  box-shadow: 0 0 0 2px map-get($custom-colors, 'stone-850');
}

.winner-avatar + .winner-avatar {
  margin-left: -7px;
}
</style>
