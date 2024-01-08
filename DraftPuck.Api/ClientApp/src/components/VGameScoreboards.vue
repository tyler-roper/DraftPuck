<script setup lang="ts">
import VGameScoreboard from '@/components/VGameScoreboard.vue'

defineProps<{
  games: Game[]
}>()
</script>

<template>
  <div>
    <div class="flex-grow-1 overflow-hidden d-flex flex-wrap p-2" style="align-items: flex-start">
      <VGameScoreboard
        v-for="game in games"
        :key="game.id"
        class="game-scoreboard m-3"
        :game="game"
        style="width: calc(50% - 2rem)"
      />
    </div>
  </div>
</template>
