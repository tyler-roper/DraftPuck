<script setup lang="ts">
//#region imports
import { addMinutes, format } from 'date-fns'
import { computed } from 'vue'
import TeamColors from '@/models/teamColorLookup'
import { getOrdinal } from '@/helpers/gameHelpers'
import GameState from '@/enums/gameState'
import PeriodType from '@/enums/periodType'
import PlayType from '@/enums/playType'
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
//#endregion

//#region props
export interface Props {
  game: Game
  player: Player
  isForPicking?: boolean
  isSelected?: boolean
  selectedPlayerCount: number
  isRemoving?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  isForPicking: false,
  isSelected: false,
  isRemoving: false
})

//aliasing props
const game = computed(() => props.game)
const player = computed(() => props.player)
const isForPicking = computed(() => props.isForPicking)
const isSelected = computed(() => props.isSelected)
const isRemoving = computed(() => props.isRemoving)
//#endregion

//#region refs
const store = useLobbyStore()
const { lobby, currentUserId, currentSystemTime } = storeToRefs(store)
//#endregion

//#region computed
const isPlayerPickableByCurrentMember = computed(() => {
  const isGameStartingWithin30Minutes = game.value.gameState === GameState.Upcoming && game.value.dateTime <= addMinutes(currentSystemTime.value, 30)
  const isGamePickable = game.value.gameState === GameState.Live || isGameStartingWithin30Minutes

  const team = playerTeam.value
  const picksPerTeam = lobby.value!.picksPerTeam
  const picksAlreadyMadeForTeam = currentMember.value.picks.filter((pick) => pick.teamId === team.id).length
  const hasPicksAvailableForTeam = picksAlreadyMadeForTeam + props.selectedPlayerCount < picksPerTeam

  return !isGameLocked.value && isGamePickable && !isPlayerPicked.value && hasPicksAvailableForTeam
})

const playerTeam = computed(() => (game.value.homeTeam.roster.some((p) => p.id === player.value.id) ? game.value.homeTeam : game.value.awayTeam))
const currentMember = computed(() => lobby.value!.members.find((member) => member.userId === currentUserId.value)!)
const isFaded = computed(
  () =>
    (isForPicking.value && !isPlayerPickableByCurrentMember.value && !isSelected.value && !isPlayerPickedByCurrentMember.value) ||
    game.value.gameState === GameState.Final
)
const isGameLocked = computed(() => !lobby.value?.gameIds.includes(game.value.id))
const isPlayerPicked = computed(() => lobby.value!.members.flatMap((member) => member.picks).some((pick) => pick.playerId === player.value.id))
const isPlayerPickedByCurrentMember = computed(() => currentMember.value.picks.some((pick) => pick.playerId === player.value.id))
const isPlayerPickedBySomeoneElse = computed(() => isPlayerPicked.value && !isPlayerPickedByCurrentMember.value)
const pickedByMember = computed(() => lobby.value!.members.find((member) => member.picks.some((pick) => pick.playerId === player.value.id)))
const teamLogo = computed(() => `/img/logos/${playerTeam.value.abbreviation}.png`)
const goalString = computed(() => {
  const goalCount = game.value.plays.reduce((count, play) => {
    if (play.type === PlayType.Goal && play.primaryPlayerId === player.value.id) count += 1
    return count
  }, 0)
  return `${goalCount} ${goalCount === 1 ? 'Goal' : 'Goals'}`
})

const gameTime = computed(() => {
  let time = format(game.value.dateTime, 'p')

  if (game.value.gameState === GameState.Upcoming) return time

  const ordinal = getOrdinal(game.value.period, game.value.periodType)

  if (game.value.gameState === GameState.Live) {
    if (game.value.periodType === PeriodType.Shootout) time = 'SO'
    else {
      time = `${game.value.timeRemainingInPeriod} - ${ordinal}`
    }
  } else if (game.value.gameState === GameState.Final) {
    time = 'Final'
    if (game.value.period > 3) time += ` (${ordinal})`
  }

  return time
})
//#endregion

//#region emitters
const emit = defineEmits(['onSelected', 'onUnpicked'])

function trySelect() {
  if (!isPlayerPickableByCurrentMember.value && !isSelected.value) return
  emit('onSelected', player.value)
}
//#endregion
</script>

<template>
  <div class="bg-stone-100" @click="trySelect">
    <div
      class="player-container"
      :class="{
        'o-50': isFaded && !isRemoving,
        'selected-for-removal': isRemoving,
        selected: isSelected,
        picked: isPlayerPickedByCurrentMember && isForPicking
      }"
      :style="{
        'border-left-color':
          isPlayerPickableByCurrentMember || (!isForPicking && isPlayerPickedByCurrentMember) || game.gameState === GameState.Final
            ? TeamColors[playerTeam.id]
            : '',
        'background-color': isPlayerPickedByCurrentMember && isForPicking ? TeamColors[playerTeam.id] : '',
        'background-image':
          isPlayerPickedByCurrentMember && isForPicking
            ? `linear-gradient(to top, rgba(0,0,0,0.1), rgba(255,255,255,0.1) 50%, rgba(255,255,255,0.2) 50%, rgba(255,255,255,0.5))`
            : ''
      }"
    >
      <div>
        <img :src="player.headshot" class="headshot" />
      </div>

      <div class="player-info flex-grow-1 d-flex">
        <div class="player-and-team">
          <div class="name fs-6">
            <span class="position-badge fs-8 me-1">{{ player.position }}</span>
            <span class="d-block" :class="{ 'text-stone-600': isPlayerPickedBySomeoneElse && !isRemoving }">
              {{ player.firstName }} {{ player.lastName }}
            </span>
          </div>
          <div class="team">
            <span class="text-stone-500">
              <span v-if="!isForPicking || game.gameState === GameState.Final">
                <img :src="teamLogo" style="width: 25px" />
                <span :class="{ 'text-stone-0': isRemoving }">{{ playerTeam.location }} {{ playerTeam.name }}</span>
              </span>
              <span v-else>
                <template v-if="isPlayerPicked">
                  <span v-if="isPlayerPickedByCurrentMember" class="text-uppercase fw-bold text-stone-0">My Pick</span>
                  <span v-else>
                    Picked by <span class="text-danger">{{ pickedByMember!.name }}</span>
                  </span>
                </template>

                <template v-else-if="isSelected">
                  <span class="text-stone-0">Selected, Not Locked In</span>
                </template>

                <template v-else-if="isGameLocked">
                  <span class="text-stone-600">Locked</span>
                </template>

                <template v-else>
                  <span class="text-success">Available</span>
                </template>
              </span>
            </span>
          </div>
        </div>

        <div
          v-if="!isRemoving"
          class="ms-auto text-stone-900"
          :class="{ 'text-stone-0': isSelected || (isPlayerPickedByCurrentMember && isForPicking) }"
        >
          <span class="fs-6 fw-bold d-block text-end" v-if="game.gameState !== GameState.Upcoming">{{ goalString }}</span>
          <span class="fs-6 fw-bold d-block text-end" v-else>{{ gameTime }}</span>
          <span class="fs-7" v-if="isForPicking || game.gameState === GameState.Upcoming">
            <span class="text-stone-400 me-1" :class="{ 'text-stone-0 o-75': isSelected || (isPlayerPickedByCurrentMember && isForPicking) }"
              >Season:</span
            >
            <span>{{ player.goals }} Goals</span>
          </span>
          <div class="text-end fs-7" v-else>
            <span>{{ gameTime }}</span>
          </div>
        </div>

        <div v-if="isRemoving" class="ms-auto text-stone-900 d-flex align-items-center">
          <a
            role="button"
            class="d-block fs-4 text-stone-0 text-decoration-none text-uppercase fw-bold py-2 px-3 bg-dark-red rounded"
            @click="emit('onUnpicked')"
          >
            <i class="fi fi-sr-trash-undo pe-2 position-relative" style="top: 3px"></i>
            <span>Unpick</span>
          </a>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.player-container {
  width: 100%;
  display: flex;
  border-bottom: 1px solid map-get($custom-colors, 'stone-200');
  align-items: center;
  border-left: 10px solid map-get($custom-colors, 'stone-600');
}

.player-container.selected {
  background-color: map-get($custom-colors, 'blue');
  border-left: 10px solid map-get($custom-colors, 'blue');
  position: relative;
  box-shadow: 0 0 10px rgba(0, 0, 0, 1);
  border-bottom: 1px solid transparent;
}

.player-container.selected-for-removal {
  background-color: map-get($custom-colors, 'red');
  border-left: 10px solid map-get($custom-colors, 'red') !important;
  position: relative;
  box-shadow: 0 0 10px rgba(0, 0, 0, 1);
  border-bottom: 1px solid transparent;
}

.player-container > * {
  padding: 0.5rem;
}

.player-container .position-badge {
  color: map-get($custom-colors, 'stone-0');
  background: map-get($custom-colors, 'stone-400');
  display: block;
  padding: 1px 5px 0px;
  border-radius: 5px;
}

.player-container .name {
  color: map-get($custom-colors, 'stone-900');
  font-weight: bold;
  display: flex;
  align-items: center;
}

.player-container.selected .name,
.player-container.selected-for-removal .name {
  color: map-get($custom-colors, 'stone-0') !important;
}

.player-container.picked {
  border-bottom: 3px solid rgba(0, 0, 0, 0.2);
  border-left: none;
  padding-left: 10px;
  position: relative;
  z-index: 2;
  box-shadow: 0 0 10px rgba(0, 0, 0, 0.3);
  text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.4);
}

.player-container.picked .position-badge {
  background: map-get($custom-colors, 'stone-0');
  color: map-get($custom-colors, 'stone-900');
}

.player-container.picked .name {
  color: map-get($custom-colors, 'stone-0');
}

.player-container.picked img.headshot {
  outline: 3px solid rgba(0, 0, 0, 0.1);
  box-shadow: 0 0 10px rgba(255, 255, 255, 0.8);
  background-color: map-get($custom-colors, 'stone-0');
}

img.headshot {
  display: block;
  width: 50px;
  height: 50px;
  background: map-get($custom-colors, 'stone-200');
  border-radius: 100%;
}

img.team-logo {
  width: 25px;
  height: 25px;
}

.team {
  display: flex;
  align-items: center;
}
</style>
