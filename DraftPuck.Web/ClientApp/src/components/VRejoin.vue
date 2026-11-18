<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import VIcon from '@/components/VIcon.vue'
import { useUserStore } from '@/stores/user'
import { useRejoin } from '@/composables/useRejoin'
import { rejoinDetails } from '@/helpers/rejoinHelpers'

/**
 * A quick way back into tonight's lobby, floating over the bottom of the home page: one live lobby opens straight
 * away, several open a list to choose from. Dismissable.
 */
const { currentUser } = storeToRefs(useUserStore())
const { lobbies, isVisible, dismiss } = useRejoin()
const router = useRouter()
const isChoosing = ref(false)

const open = (lobby: LobbySummary) => router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } })

function rejoin() {
  if (lobbies.value.length === 1) open(lobbies.value[0]!)
  else isChoosing.value = true
}
</script>

<template>
  <Transition name="rise">
    <div v-if="isVisible && !isChoosing" class="rejoin-bar">
      <button class="rejoin-button" @click="rejoin">
        <VIcon prefix="sr" icon="hockey-puck" class="fs-4 me-3" />
        <span class="d-block text-start min-w-0 flex-grow-1">
          <template v-if="lobbies.length === 1">
            <span class="d-block fw-bold text-uppercase">Rejoin {{ lobbies[0]!.joinCode }}</span>
            <span class="d-block fs-7 text-truncate">{{ rejoinDetails(lobbies[0]!, currentUser?.id) }}</span>
          </template>
          <template v-else>
            <span class="d-block fw-bold text-uppercase">Rejoin a lobby</span>
            <span class="d-block fs-7">{{ lobbies.length }} live lobbies</span>
          </template>
        </span>
        <VIcon prefix="sr" icon="angle-right" class="fs-5" />
      </button>
      <button class="dismiss" title="Dismiss" aria-label="Dismiss" @click="dismiss">
        <VIcon prefix="sr" icon="cross-small" class="fs-4" />
      </button>
    </div>
  </Transition>

  <div v-if="isChoosing" class="modal-overlay" @click="isChoosing = false">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block">Rejoin a Lobby</span>
        <a role="button" class="text-uppercase text-primary text-decoration-none" @click="isChoosing = false">Close</a>
      </div>
      <div class="modal-body dark">
        <button v-for="lobby in lobbies" :key="lobby.joinCode" class="lobby-option" @click="open(lobby)">
          <span class="code">{{ lobby.joinCode }}</span>
          <span class="flex-grow-1 min-w-0 text-start fs-7 text-stone-300 text-truncate">{{ rejoinDetails(lobby, currentUser?.id) }}</span>
          <VIcon prefix="sr" icon="angle-right" class="text-stone-400" />
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.rejoin-bar {
  position: fixed;
  left: 16px;
  right: 16px;
  bottom: calc(16px + env(safe-area-inset-bottom, 0px));
  max-width: 480px;
  margin: 0 auto;
  z-index: 1000;
  display: flex;
  align-items: stretch;
  border-radius: 15px;
  overflow: hidden;
  background-color: map-get($custom-colors, 'primary');
  box-shadow: 0 6px 24px rgba(0, 0, 0, 0.6);
}

.rejoin-button,
.dismiss {
  border: none;
  background: transparent;
  color: map-get($custom-colors, 'stone-900');
}

.rejoin-button {
  flex-grow: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  padding: 12px 8px 12px 16px;
}

.dismiss {
  padding: 0 14px;
  border-left: 1px solid rgba(0, 0, 0, 0.15);
}

.modal-body.dark {
  color: map-get($custom-colors, 'stone-0');
  background: map-get($custom-colors, 'stone-850');
}

.lobby-option {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  margin-bottom: 8px;
  border-radius: 12px;
  border: 1px solid map-get($custom-colors, 'stone-700');
  background-color: map-get($custom-colors, 'stone-800');
  color: map-get($custom-colors, 'stone-0');
}

.lobby-option:last-child {
  margin-bottom: 0;
}

.lobby-option .code {
  font-weight: bold;
  letter-spacing: 3px;
  color: map-get($custom-colors, 'primary');
}

.min-w-0 {
  min-width: 0;
}

.rise-enter-active,
.rise-leave-active {
  transition:
    transform 0.25s ease,
    opacity 0.25s ease;
}

.rise-enter-from,
.rise-leave-to {
  transform: translateY(24px);
  opacity: 0;
}
</style>
