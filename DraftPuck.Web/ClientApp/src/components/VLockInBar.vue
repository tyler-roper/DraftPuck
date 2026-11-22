<script setup lang="ts">
import GamePickableStatus from '@/enums/gamePickableStatus'
import { lightTeamLogoUrl } from '@/helpers/teamLogos'

defineProps<{
  status: GamePickableStatus
  team: GameTeam
  // Picks left for the team after the selected players.
  picksRemaining: number
  selectedCount: number
  // How long until picking opens, while it's upcoming.
  countdown: string
  // Waiting for the server to answer a lock-in.
  isPending: boolean
  // Briefly true after the team's last pick locks in, for the animation.
  justLockedIn: boolean
}>()

const emit = defineEmits<{ lockIn: [] }>()
</script>

<template>
  <div class="lock-in shadow" :class="{ 'just-locked-in': justLockedIn }">
    <div
      v-if="status === GamePickableStatus.Upcoming"
      class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-300 d-flex justify-content-center align-items-center"
    >
      <span class="text-stone-600">Picks in</span>
      <i class="fi fi-sr-alarm-clock d-block fs-1 me-2 ms-4 mb-n1 pt-1"></i>
      <span class="d-block fw-bold text-uppercase">{{ countdown }}</span>
    </div>
    <div
      v-else-if="status === GamePickableStatus.GameComplete"
      class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-600 d-flex justify-content-center align-items-center"
    >
      <i class="fi fi-sr-time-check d-block fs-1 me-2 p-relative mb-n1 mt-1"></i>
      <span class="d-block fw-bold text-uppercase">Game Over</span>
    </div>
    <div
      v-else-if="status === GamePickableStatus.Locked"
      class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-600 d-flex justify-content-center align-items-center"
    >
      <i class="fi fi-sr-lock d-block fs-1 me-2"></i>
      <span class="d-block fw-bold text-uppercase">Game Locked</span>
    </div>
    <div
      v-else-if="picksRemaining <= 0 && selectedCount === 0"
      class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-primary d-flex justify-content-center align-items-center"
    >
      <i class="fi fi-sr-lock d-block fs-1 me-2"></i>
      <span class="d-block fw-bold text-uppercase">Locked In!</span>
    </div>
    <template v-else>
      <div class="text-uppercase d-flex align-items-center px-2">
        <div class="fs-4">Picks Remaining:</div>
        <div class="py-2 ms-2 fs-1 fw-bold d-flex align-items-center">
          <img style="width: 33px; height: 37px" :src="lightTeamLogoUrl(team)" />
          <div>{{ picksRemaining }}</div>
        </div>
      </div>
      <div>
        <button
          class="btn btn-primary fw-bold fs-3 h-100 rounded-0 text-uppercase position-relative"
          :class="{ emphasized: selectedCount > 0 }"
          :disabled="selectedCount === 0 || isPending"
          @click="emit('lockIn')"
        >
          <!-- While pending, the label stays (hidden) so the button keeps its size, and a spinner sits centered over it. -->
          <span :class="{ invisible: isPending }">Lock In</span>
          <span v-if="isPending" class="position-absolute top-50 start-50 translate-middle d-flex">
            <span class="spinner-border" role="status" aria-label="Locking in"></span>
          </span>
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.lock-in {
  background: map-get($custom-colors, 'stone-900');
  display: flex;
  justify-content: space-between;
}

.lock-in.just-locked-in {
  animation: puff-in-center 0.7s cubic-bezier(1, 0, 0, 1) both;
}

.lock-in button {
  transition: 0.3s;
  background: linear-gradient(map-get($custom-colors, 'amber-300'), map-get($custom-colors, 'primary'));
  transform: translateY(0);
  border: none;
  color: map-get($custom-colors, 'stone-800');
}

.lock-in button.emphasized {
  animation: pulsate 2s ease-in-out infinite;
  position: relative;
  text-shadow: 1px 1px 0 rgba(255, 255, 255, 0.7);
}

@keyframes pulsate {
  0% {
    box-shadow: 0 0 3px map-get($custom-colors, 'amber-300');
    transform: translateY(0);
  }

  50% {
    box-shadow: 0 0 10px map-get($custom-colors, 'amber-300');
    transform: translateY(-6px);
  }

  100% {
    box-shadow: 0 0 3px map-get($custom-colors, 'amber-300');
    transform: translateY(0);
  }
}

@keyframes puff-in-center {
  0% {
    transform: scale(5) translateY(-100px);
    filter: blur(4px);
    opacity: 0;
  }

  100% {
    transform: scale(1);
    filter: blur(0px);
    opacity: 1;
  }
}
</style>
