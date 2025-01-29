<script setup lang="ts">
import VGameScoreboard from '@/components/VGameScoreboard.vue'
import GameState from '@/enums/gameState'
import { compareAsc } from 'date-fns'
import { computed } from 'vue'
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'

const props = defineProps<{
  games: Game[]
}>()

const store = useLobbyStore()

const { lobby } = storeToRefs(store)

const sortedGames = computed(() =>
  [...props.games].sort((a, b) => {
    if (a.gameState === GameState.Final) return 1
    if (b.gameState === GameState.Final) return -1
    if (lobby.value?.gameIds.includes(a.id) && !lobby.value?.gameIds.includes(b.id)) return -1
    if (!lobby.value?.gameIds.includes(a.id) && lobby.value?.gameIds.includes(b.id)) return 1
    return compareAsc(a.dateTime, b.dateTime)
  })
)
</script>

<template>
  <div>
    <div class="flex-grow-1 overflow-hidden d-flex flex-wrap p-2" style="align-items: flex-start">
      <VGameScoreboard v-for="game in sortedGames" :key="game.id" class="game-scoreboard m-3" :game="game" style="width: calc(50% - 2rem)" />
    </div>
  </div>
</template>
