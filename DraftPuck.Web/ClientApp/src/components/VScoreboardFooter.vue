<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import VScoreboardRosters from '@/components/VScoreboardRosters.vue'
import { useLobbyStore } from '@/stores/lobby'
import { useSystemStore } from '@/stores/system'
import { hasPicksLeftInGame, isGameInLobby } from '@/helpers/pickRules'

/** Under the scores: the Rosters toggle and whether the viewer can pick in this game (or when they can). */
const props = defineProps<{
  game: Game
}>()

const { lobby, currentMember } = storeToRefs(useLobbyStore())
const { currentSystemTime } = storeToRefs(useSystemStore())

const isRosterVisible = ref(false)
const isLobbyGame = computed(() => !!lobby.value && isGameInLobby(lobby.value, props.game))
// currentSystemTime ticks every second, so this flips when picking opens without a timer of its own.
const hasPickingOpened = computed(() => currentSystemTime.value >= props.game.pickingOpensUtc)
const canPick = computed(
  () => !!lobby.value && !!currentMember.value && hasPicksLeftInGame(lobby.value, currentMember.value, props.game, currentSystemTime.value)
)
</script>

<template>
  <div class="d-flex px-3 py-2 footer-bar" style="height: 38px">
    <a
      v-if="game.playerSummaries.length"
      role="button"
      class="text-stone-900 ps-0 pe-0 d-flex fw-bold uppercase text-decoration-none"
      style="align-self: flex-start; border-bottom: 2px solid #1c1917; padding-top: 2px"
      @click="isRosterVisible = !isRosterVisible"
    >
      <span class="d-block" style="margin-top: -3px; margin-bottom: -3px">Rosters</span>
      <i class="d-block mb-n3 fi me-n1" :class="!isRosterVisible ? 'fi-sr-caret-right' : 'fi-sr-caret-down'"></i>
    </a>
    <span v-else class="text-stone-400 small mt-1"> No Rosters Yet </span>

    <span v-if="canPick" class="d-flex align-items-center small text-uppercase ms-3" style="margin-top: 2px">
      <span class="d-block mb-n1"><i class="fs-7 fi fi-sr-exclamation text-danger"></i></span>
      <span class="d-block mb-n1 ms-1 fw-bold">Picks Available</span>
    </span>
    <span v-if="!hasPickingOpened && isLobbyGame" class="text-stone-600 small mt-1 mb-n1 ms-3"
      >Picks open @ <strong>{{ format(game.pickingOpensUtc, 'p') }}</strong></span
    >
    <span v-if="!isLobbyGame" class="text-stone-600 small mt-1 mb-n1 ms-3">
      <i class="fi fi-sr-lock me-1"></i>The lobby admin has disabled picks for this game.</span
    >
  </div>
  <VScoreboardRosters v-if="isRosterVisible" :game="game" />
</template>

<style scoped>
.footer-bar {
  position: relative;
  z-index: 2;
  box-shadow: 0 5px 5px rgba(0, 0, 0, 0.2);
}
</style>
