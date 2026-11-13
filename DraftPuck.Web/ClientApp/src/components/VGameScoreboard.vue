<script setup lang="ts">
import PickRequest from '@/models/pickRequest'
import { useLobbyStore } from '@/stores/lobby'
import { format } from 'date-fns'
import { canPickPlayer, canRemovePick, findPick, hasPicksLeftInGame, isGameInLobby } from '@/helpers/pickRules'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { storeToRefs } from 'pinia'
import { computed, ref } from 'vue'
import { useToast } from 'vue-toastification'
import TeamColors from '@/models/teamColorLookup'
import { getOrdinal } from '@/helpers/gameHelpers'
import GameState from '@/enums/gameState'
import PeriodType from '@/enums/periodType'
import PlayType from '@/enums/playType'
import TeamSituation from '@/enums/teamSituation'
import { useSystemStore } from '@/stores/system'
import { escapeHtml } from '@/helpers/htmlHelpers'

const props = defineProps<{
  game: Game
}>()

//aliasing props
const game = computed(() => props.game)

//data
const toast = useToast()
const lobbyStore = useLobbyStore()
const systemStore = useSystemStore()
const { currentSystemTime } = storeToRefs(systemStore)

const { lobby, currentUserId } = storeToRefs(lobbyStore)
const { makePicks, removePick } = lobbyStore

const isRosterVisible = ref(false)
// Players sent to the server, until it answers.
const pendingPlayerIds = ref(new Set<number>())

//computed
const gameIsPickable = computed(() => !!lobby.value && isGameInLobby(lobby.value, game.value))
// currentSystemTime ticks every second, so this flips when picking opens without a timer of its own.
const hasPickingOpened = computed(() => currentSystemTime.value >= game.value.pickingOpensUtc)
const isStarted = computed(() => game.value.gameState !== GameState.Upcoming)
const isInProgress = computed(() => game.value.gameState === GameState.Live)
const isOver = computed(() => game.value.gameState === GameState.Final)
const isExtraTime = computed(() => game.value.period > 3)
const isThirdPeriod = computed(() => game.value.period === 3)
const isCloseGame = computed(() => Math.abs(game.value.awayTeam.score - game.value.homeTeam.score) <= 1)
const isLateInPeriod = computed(() => game.value.minutesRemainingInPeriod < 5)
const teams = computed(() => ({ home: game.value.homeTeam, away: game.value.awayTeam }))
const pickedPlayers = computed(
  () =>
    lobby.value?.members.reduce((playerIdLookup: { [key: number]: string }, member) => {
      member.picks.filter((pick) => pick.gameId === game.value.id).forEach((pick) => (playerIdLookup[pick.playerId] = member.name))
      return playerIdLookup
    }, {}) ?? {}
)
const currentUser = computed(() => lobby.value!.members.find((m) => m.userId === currentUserId.value))
const currentUserCanPick = computed(
  () => !!currentUser.value && hasPicksLeftInGame(lobby.value!, currentUser.value, game.value, currentSystemTime.value)
)
const pickTimeFormatted = computed(() => format(game.value.pickingOpensUtc, 'p'))

const time = computed(() => {
  let time = format(game.value.dateTime, 'p')

  if (!isStarted.value) return time

  const ordinal = getOrdinal(game.value.period, game.value.periodType)

  if (isInProgress.value) {
    if (game.value.periodType === PeriodType.Shootout) time = 'SO'
    else {
      time = `${game.value.timeRemainingInPeriod} - ${ordinal}`
    }
  } else if (isOver.value) {
    time = 'Final'
    if (game.value.period > 3) time += ` (${ordinal})`
  }

  return time
})

const strengthString = computed(() => {
  const homeStrength = game.value.homeTeam.strength
  const awayStrength = game.value.awayTeam.strength
  const result = `${Math.max(homeStrength, awayStrength)}-on-${Math.min(homeStrength, awayStrength)}`
  return result === `5-on-4` ? 'PP' : result
})

//methods
async function getPickIdAndRemovePick(playerId: number) {
  const found = lobby.value && findPick(lobby.value, game.value.id, playerId)
  if (!found) return

  try {
    await removePick(found.pick.id)
  } catch (error) {
    toast.error(getErrorMessage(error, "Couldn't remove that pick. Please try again."))
  }
}

/** Picks a player for the current user. Pending until the server answers; a refusal shows the server's reason. */
async function pick(player: Player, teamId: number) {
  if (!currentUser.value || pendingPlayerIds.value.has(player.id)) return

  pendingPlayerIds.value = new Set([...pendingPlayerIds.value, player.id])
  const [{ error }] = await makePicks([new PickRequest(currentUser.value.id, player.id, game.value.id, teamId)])
  pendingPlayerIds.value = new Set([...pendingPlayerIds.value].filter((id) => id !== player.id))

  if (error) toast.error(`${player.firstName} ${player.lastName}: ${error}`)
}

//#region helpers
function getGameTimeClasses(): { [key: string]: boolean } {
  if (!isInProgress.value) return {}

  return {
    'is-live': isInProgress.value,
    'is-critical': isExtraTime.value || (isThirdPeriod.value && isCloseGame.value && isLateInPeriod.value)
  }
}

function getLogo(team: GameTeam) {
  return `/img/logos/${team.abbreviation}.png`
}

function getLightLogo(team: GameTeam) {
  if (team.abbreviation !== 'TBL') return getLogo(team)
  return `/img/logos/${team.abbreviation}_LIGHT.png`
}

function isHome(team: GameTeam) {
  return team.id === game.value.homeTeam.id
}

function getOpponent(team: GameTeam) {
  return isHome(team) ? game.value.awayTeam : game.value.homeTeam
}

function isTeamLosing(team: GameTeam) {
  return team.score < getOpponent(team).score
}

function isTeamWinning(team: GameTeam) {
  return team.score > getOpponent(team).score
}

function teamWon(team: GameTeam) {
  return isOver.value && isTeamWinning(team)
}

function getScoreByPeriod(team: GameTeam, period: number) {
  const linescorePeriod = game.value.goalsByPeriod.find((p) => p.number === period)
  if (!linescorePeriod) return 0

  return isHome(team) ? linescorePeriod.homeGoals : linescorePeriod.awayGoals
}

function getScorersByTeam(team: GameTeam) {
  return game.value.plays.reduce(
    (acc, play) => {
      if (play.type !== PlayType.Goal) return acc

      if (play.primaryTeamId === team.id) acc[play.primaryPlayerId!] = (acc[play.primaryPlayerId!] ?? 0) + 1

      return acc
    },
    {} as { [key: number]: number }
  )
}

function getScorerStringByTeam(team: GameTeam) {
  const scorers = Object.entries(getScorersByTeam(team)).map(([playerId, goals]) => {
    const player = team.roster.find((player) => player.id === Number(playerId))
    if (!player) return ''

    let output = escapeHtml(`${player.firstName[0]} ${player.lastName}`)
    if (goals > 1) output += ` (${goals})`

    return output
  })

  return scorers.join(', ')
}

function getPickerName(playerId: number) {
  return pickedPlayers.value[playerId]
}

function isPlayerPicked(playerId: number) {
  return !!getPickerName(playerId)
}


function isCurrentUserPick(player: Player) {
  return !!currentUser.value?.picks.some((p) => p.gameId === game.value.id && p.playerId === player.id)
}

function currentUserCanPickPlayer(player: Player, team: GameTeam) {
  return (
    !!currentUser.value &&
    !pendingPlayerIds.value.has(player.id) &&
    canPickPlayer(lobby.value!, currentUser.value, game.value, { id: player.id, teamId: team.id }, currentSystemTime.value)
  )
}

function currentUserCanRemovePick(player: Player) {
  const found = findPick(lobby.value!, game.value.id, player.id)
  return !!found && !!currentUser.value && canRemovePick(lobby.value!, game.value, found.pick, currentUser.value)
}

function getFriendlyPosition(position: string) {
  if (position === 'L' || position === 'R') return position + 'W'
  return position
}

//#endregion
</script>

<template>
  <div class="bg-stone-200 text-stone-800 overflow-hidden rounded position-relative" :class="getGameTimeClasses()">
    <table class="score-table">
      <!-- HEADER -->
      <thead>
        <tr>
          <th style="width: 290px">
            <span class="fw-bold">{{ time }}</span>
          </th>

          <th style="width: 45px" v-for="(period, idx) in game.goalsByPeriod" :key="idx">
            <span v-if="isStarted" class="fw-bold">{{ period.number > 3 ? getOrdinal(period.number, period.periodType) : period.number }}</span>
          </th>

          <th style="width: 45px" v-if="game.goalsByPeriod.length === 0"><span v-if="isStarted" class="fw-bold">1</span></th>
          <th style="width: 45px" v-if="game.goalsByPeriod.length <= 1"><span v-if="isStarted" class="fw-bold">2</span></th>
          <th style="width: 45px" v-if="game.goalsByPeriod.length <= 2"><span v-if="isStarted" class="fw-bold">3</span></th>

          <th style="width: 45px"></th>
          <th></th>
        </tr>
      </thead>

      <!-- TEAMS -->
      <tbody>
        <tr v-for="team in teams" :key="team.id">
          <td
            class="bg-stone-150"
            :class="{ 'text-stone-0': teamWon(team) }"
            :style="{ 'background-color': teamWon(team) ? `${TeamColors[team.id]} !important` : '' }"
          >
            <div class="d-flex align-items-center">
              <img
                :src="teamWon(team) ? getLightLogo(team) : getLogo(team)"
                style="width: 50px; height: 50px"
                class="d-block"
                :style="{ filter: !isStarted || (isOver && isTeamLosing(team)) ? 'grayscale(1)' : '' }"
              />
              <div class="ms-2 team-name">
                <span class="d-block text-nowrap">{{ team.location }}</span>
                <span class="d-block text-nowrap text-uppercase fw-bold mt-n1">{{ team.name }}</span>
              </div>
              <div class="ms-2 team-abr">
                <span class="d-block d-block text-uppercase fw-bold">{{ team.abbreviation }}</span>
              </div>
              <div v-if="!isOver" class="ms-auto fw-bold fs-8 text-stone-0 d-flex">
                <span
                  v-for="situation in team.situations"
                  class="p-1 rounded text-uppercase ms-1 text-nowrap"
                  style="line-height: 12px"
                  :key="situation"
                  :style="{ 'background-color': TeamColors[team.id] }"
                >
                  <span v-if="situation === TeamSituation.PowerPlay">{{ strengthString }}</span>
                  <span v-if="situation === TeamSituation.EmptyNet">EN</span>
                </span>
              </div>
            </div>
          </td>

          <td v-for="(period, idx) in game.goalsByPeriod" :key="idx">
            <span v-if="isStarted && period.number <= game.period">{{ getScoreByPeriod(team, period.number) }}</span>
          </td>

          <td v-if="game.goalsByPeriod.length === 0"></td>
          <td v-if="game.goalsByPeriod.length <= 1"></td>
          <td v-if="game.goalsByPeriod.length <= 2"></td>

          <td>
            <span v-if="isStarted" class="fw-bold fs-6">{{ team.score }}</span>
          </td>

          <td class="ps-4" v-html="getScorerStringByTeam(team)"></td>
        </tr>
      </tbody>

      <!-- FOOTER -->
      <tfoot>
        <tr>
          <td colspan="100" class="bg-stone-100 p-0">
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

              <span v-if="!game.playerSummaries.length" class="text-stone-400 small mt-1"> No Rosters Yet </span>

              <span v-if="currentUserCanPick" class="d-flex align-items-center small text-uppercase ms-3" style="margin-top: 2px">
                <span class="d-block mb-n1"><i class="fs-7 fi fi-sr-exclamation text-danger"></i></span>
                <span class="d-block mb-n1 ms-1 fw-bold">Picks Available</span>
              </span>
              <span v-if="!hasPickingOpened && gameIsPickable" class="text-stone-600 small mt-1 mb-n1 ms-3"
                >Picks open @ <strong>{{ pickTimeFormatted }}</strong></span
              >
              <span v-if="!gameIsPickable" class="text-stone-600 small mt-1 mb-n1 ms-3">
                <i class="fi fi-sr-lock me-1"></i>The lobby admin has disabled picks for this game.</span
              >
            </div>
            <div v-if="isRosterVisible">
              <div class="row inset-shadow">
                <div v-for="team in Object.values(teams)" :key="team.id" class="roster-split col-lg-6 col-12">
                  <div class="px-3 py-2 fw-bold text-stone-0 d-flex align-items-center" :style="{ 'background-color': TeamColors[team.id] }">
                    <img class="d-block" :src="getLightLogo(team)" style="width: 40px; height: 40px" />
                    <div class="ms-3">
                      <span class="d-block text-uppercase">{{ team.name }}</span>
                      <span class="d-block font-weight-normal mt-n1">Season Stats</span>
                    </div>
                  </div>
                  <table style="width: 100%" class="roster-table">
                    <thead>
                      <tr>
                        <th colspan="3"></th>
                        <th class="text-right" style="width: 40px">GP</th>
                        <th class="text-right" style="width: 40px">G</th>
                        <th class="text-right" style="width: 40px">A</th>
                        <th class="text-right" style="width: 40px">P</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="player in team.roster.filter((p) => p.position !== 'G')" :key="player.id">
                        <td class="text-right" style="width: 20px">{{ getFriendlyPosition(player.position) }}</td>
                        <td class="text-right" style="width: 40px">#{{ player.number }}</td>
                        <td>
                          <span class="text-stone-700" :class="{ 'fw-bold': isCurrentUserPick(player) }">{{
                            `${player.firstName} ${player.lastName}`
                          }}</span>
                          <a
                            v-if="gameIsPickable && currentUserCanPickPlayer(player, team)"
                            role="button"
                            class="btn btn-primary py-0 px-1 text-decoration-none fs-8 fw-bold ms-2"
                            style="height: 12px; line-height: 12px; margin-top: -1px"
                            @click="pick(player, team.id)"
                          >
                            PICK
                          </a>

                          <span
                            v-if="isPlayerPicked(player.id)"
                            class="badge text-upperase ms-2"
                            :class="isCurrentUserPick(player) ? 'bg-danger' : 'bg-blue'"
                          >
                            {{ isCurrentUserPick(player) ? '(You)' : getPickerName(player.id) }}
                            <a
                              v-if="currentUserCanRemovePick(player)"
                              role="button"
                              class="ms-1"
                              @click="getPickIdAndRemovePick(player.id)"
                              :class="{ 'text-white': isCurrentUserPick }"
                              >x</a
                            >
                          </span>
                        </td>
                        <td class="text-right">{{ player.gamesPlayed }}</td>
                        <td class="text-right">{{ player.goals }}</td>
                        <td class="text-right">{{ player.assists }}</td>
                        <td class="text-right">{{ player.points }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </td>
        </tr>
      </tfoot>
    </table>
  </div>
</template>

<style scoped>
.roster-table > thead > tr > th,
.roster-table > thead > tr > td,
.roster-table > tbody > tr > th,
.roster-table > tbody > tr > td {
  padding: 3px 10px;
}

.roster-split {
  background-color: #ebebeb;
}

.roster-table > thead > tr > th {
  background-color: rgba(0, 0, 0, 0.02);
}

.roster-table > tbody > tr:nth-child(even) > td {
  background-color: rgba(0, 0, 0, 0.01);
}

.footer-bar {
  position: relative;
  z-index: 2;
  box-shadow: 0 5px 5px rgba(0, 0, 0, 0.2);
}
</style>
@/services/NhlService
