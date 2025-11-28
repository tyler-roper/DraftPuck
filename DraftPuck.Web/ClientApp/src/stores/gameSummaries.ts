import { defineStore } from 'pinia'
import { ref } from 'vue'
import GameService from '@/services/GameService'
import GameState from '@/enums/gameState'
import { compareAsc } from 'date-fns'

export const useGameSummariesStore = defineStore('gameSummaries', () => {
  const gameSummaries = ref<Array<GameSummary>>([])
  const hasLoadedSummaries = ref(false)
  const isLoadingSummaries = ref(false)

  async function loadGameSummaries() {
    if (hasLoadedSummaries.value) return

    isLoadingSummaries.value = true
    try {
      const summaries = await GameService.getAllGameSummaries()
      gameSummaries.value = summaries
        .filter((g) => g.gameState !== GameState.Final)
        .sort((a, b) => compareAsc(a.dateTime, b.dateTime))
      hasLoadedSummaries.value = true
    } finally {
      isLoadingSummaries.value = false
    }
  }

  return {
    gameSummaries,
    hasLoadedSummaries,
    isLoadingSummaries,
    loadGameSummaries
  }
})
