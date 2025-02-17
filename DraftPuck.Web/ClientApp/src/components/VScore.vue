<script setup lang="ts">
// #region imports
import { computed } from 'vue'
import { addMinutes, format } from 'date-fns'
import GamePickableStatus from '@/enums/gamePickableStatus'
import GameState from '@/enums/gameState'
import { getOrdinal } from '@/helpers/gameHelpers'
import PeriodType from '@/enums/periodType'
import { storeToRefs } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
// #endregion

// #region props
const props = withDefaults(
  defineProps<{
    game: Game
    isSelected: boolean
  }>(),
  {
    isSelected: false
  }
)
// #endregion

// #region data
const store = useLobbyStore()
const { lobby, currentUserId, currentSystemTime } = storeToRefs(store)
// #endregion

// #region computed
const game = computed(() => props.game)
const currentMember = computed(() => lobby.value!.members.find((member) => member.userId === currentUserId.value)!)
const isSelected = computed(() => props.isSelected)
const isLobbyGame = computed(() => lobby.value?.gameIds.includes(game.value.id))
const isGameStarted = computed(() => game.value.gameState !== GameState.Upcoming)
const isGameInProgress = computed(() => game.value.gameState === GameState.Live)
const isGameOver = computed(() => game.value.gameState === GameState.Final)

const currentMemberHasMadeAllPicksForGame = computed(() => {
  const currentMemberPicks = currentMember.value.picks
  const currentMemberPicksForThisGame = currentMemberPicks.filter(
    (pick) =>
      game.value.homeTeam.roster.some((rosterPlayer) => rosterPlayer.id === pick.playerId) ||
      game.value.awayTeam.roster.some((rosterPlayer) => rosterPlayer.id === pick.playerId)
  )

  return currentMemberPicksForThisGame.length < lobby.value!.picksPerTeam * 2
})

const gamePickableStatusForCurrentMember = computed(() => {
  if (!isLobbyGame.value) return GamePickableStatus.Locked
  if (!isGameStarted.value && game.value.dateTime > addMinutes(currentSystemTime.value, 30)) return GamePickableStatus.Upcoming
  if (isGameOver.value) return GamePickableStatus.GameComplete

  return currentMemberHasMadeAllPicksForGame.value ? GamePickableStatus.PicksAvailable : GamePickableStatus.PicksMade
})

const gamePickableStatusForCurrentMemberAsString = computed(() => {
  return GamePickableStatus[gamePickableStatusForCurrentMember.value]
})

const gamePickableViewModel = computed(() => {
  const status = gamePickableStatusForCurrentMember.value

  if (status === GamePickableStatus.Locked)
    return {
      text: 'Locked',
      icon: 'fi-sr-lock',
      color: 'stone-500'
    }

  if (status === GamePickableStatus.Upcoming)
    return {
      text: format(addMinutes(game.value.dateTime, -30), 'p'),
      icon: 'fi-br-alarm-clock',
      color: 'stone-300'
    }

  if (status === GamePickableStatus.GameComplete)
    return {
      text: 'Game Over',
      color: 'stone-500'
    }

  if (status === GamePickableStatus.PicksAvailable)
    return {
      text: 'Pick Now',
      color: 'primary'
    }

  if (status === GamePickableStatus.PicksMade)
    return {
      text: 'Picked',
      color: 'success',
      icon: 'fi-ss-check-circle'
    }

  return {}
})

const timeString = computed(() => {
  let time = format(game.value.dateTime, 'p')

  if (!isGameStarted.value) return time

  const ordinal = getOrdinal(game.value.period, game.value.periodType)

  if (isGameInProgress.value) {
    if (game.value.periodType === PeriodType.Shootout) time = 'SO'
    else {
      time = `${game.value.timeRemainingInPeriod} - ${ordinal}`
    }
  } else if (isGameOver.value) {
    time = 'Final'
    if (game.value.period > 3) time += ` (${ordinal})`
  }

  return time
})
// #endregion

// #region methods

// #endregion

// #region helpers
const getLogo = (team: GameTeam) => `/img/logos/${team.abbreviation}.png`
const getLightLogo = (team: GameTeam) => (team.abbreviation !== 'TBL' ? getLogo(team) : `/img/logos/${team.abbreviation}_LIGHT.png`)

// #endregion
</script>

<template>
  <div class="game py-1 px-2" :class="{ 'o-50': !isLobbyGame || isGameOver, selected: isSelected, [gamePickableStatusForCurrentMemberAsString.toLowerCase()]: true }">
    <div class="game-time px-2 mb-1 fs-8 bg-stone-700 d-inline-block">{{ timeString }}</div>
    <div class="game-matchup">
      <div class="d-flex justify-content-between align-items-center">
        <div>
          <img :src="getLightLogo(game.awayTeam)" style="width: 25px" />
          <span>{{ game.awayTeam.abbreviation }}</span>
        </div>
        <div>
          <span class="fw-bold">{{ game.awayTeam.score }}</span>
        </div>
      </div>
      <div class="d-flex justify-content-between align-items-center">
        <div>
          <img :src="getLightLogo(game.homeTeam)" style="width: 25px" />
          <span>{{ game.homeTeam.abbreviation }}</span>
        </div>
        <div>
          <span class="fw-bold">{{ game.homeTeam.score }}</span>
        </div>
      </div>
    </div>
    <div class="game-pick-status text-center d-flex align-items-center justify-content-center mb-n1" :class="`text-${gamePickableViewModel.color}`">
      <i v-if="gamePickableViewModel.icon" class="fi d-block mt-1 me-1 fs-9" :class="gamePickableViewModel.icon"></i>
      <span class="fw-bold d-block">{{ gamePickableViewModel.text }}</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.game {
  flex-grow: 0;
  flex-shrink: 0;
  width: 100px;
  border-right: 1px solid map-get($custom-colors, 'stone-600');
  transition: 0.15s all;
  padding-top: 8px !important;
  position: relative;
  padding-bottom: 12px !important;
}

.game::after {
  content: '';
  position: absolute;
  bottom: 0;
  width: 100%;
  left: 0;
  height: 8px;
}

.game.selected::after {
  background-color: map-get($custom-colors, 'primary');
}

.game-time {
  border-radius: 5px;
  line-height: 18px;
  padding-top: 2px;
}

.game:hover {
  opacity: 1;
  cursor: pointer;
}

.game.selected {
  opacity: 1;
}

.game.picksavailable {
  animation: glow-pulse 4s ease-in-out infinite;
}

@keyframes glow-pulse {
  0% {
    box-shadow: 0 0 15px map-get($custom-colors, 'stone-800') inset;
  }
  50% {
    box-shadow: 0 0 15px map-get($custom-colors, 'primary') inset;
  }
  100% {
    box-shadow: 0 0 15px map-get($custom-colors, 'stone-800') inset;
  }
}
</style>
