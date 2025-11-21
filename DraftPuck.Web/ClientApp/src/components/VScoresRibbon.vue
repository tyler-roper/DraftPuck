<script setup lang="ts">
//#region imports
import VScore from '@/components/VScore.vue'
//#endregion

//#region props
defineProps<{
  games: Game[]
  selectedGame?: Game
}>()
//#endregion

//#region emitters
const emit = defineEmits(['onScoreClicked'])

function onScoreClicked(game: Game) {
  emit('onScoreClicked', game)
}
//#endregion
</script>

<template>
  <div class="visible-games-container">
    <div class="scrollable-games-container">
      <VScore v-for="game in games" :key="game.id" :game="game" :is-selected="selectedGame?.id === game.id" @click="onScoreClicked(game)" />
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.visible-games-container {
  width: 100%;
  overflow-x: scroll;
  border-bottom: 2px solid map-get($custom-colors, 'stone-600');
  flex-shrink: 0;
}

.scrollable-games-container {
  display: flex;
}
</style>
