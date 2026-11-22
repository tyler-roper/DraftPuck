<script setup lang="ts">
import { computed } from 'vue'
import VScoreboardLinescore from '@/components/VScoreboardLinescore.vue'
import VScoreboardFooter from '@/components/VScoreboardFooter.vue'
import GameState from '@/enums/gameState'
import { isCriticalMoment } from '@/helpers/scoreboard'

/** A game's scoreboard on the lobby page (desktop): scores by period, scorers, and its rosters to pick from. */
const props = defineProps<{
  game: Game
}>()

// The site's score-table styles highlight a live game, and pulse at a critical moment.
const liveClasses = computed(() => (props.game.gameState === GameState.Live ? { 'is-live': true, 'is-critical': isCriticalMoment(props.game) } : {}))
</script>

<template>
  <div class="bg-stone-200 text-stone-800 overflow-hidden rounded position-relative" :class="liveClasses">
    <table class="score-table">
      <VScoreboardLinescore :game="game" />
      <tfoot>
        <tr>
          <td colspan="100" class="bg-stone-100 p-0">
            <VScoreboardFooter :game="game" />
          </td>
        </tr>
      </tfoot>
    </table>
  </div>
</template>
