<script setup lang="ts">
import PickRequest from '@/models/pickRequest'
import { useLobbyStore } from '@/stores/lobby'
import { addMinutes, formatDuration, intervalToDuration, format } from 'date-fns'
import { storeToRefs } from 'pinia'
import { computed, ref } from 'vue'
import { useToast } from 'vue-toastification'
import type Bot from '@/models/bot'
import BotPickStyle from '@/enums/botPickStyle'
import BotFallbackPickStrategy from '@/enums/botFallbackPickStrategy'
import TeamColors from '@/models/teamColorLookup'
import { getOrdinal } from '@/helpers/gameHelpers'
import GameState from '@/enums/gameState'
import PeriodType from '@/enums/periodType'
import PlayType from '@/enums/playType'
import TeamSituation from '@/enums/teamSituation'

const props = defineProps<{
  game: Game
}>()

//aliasing props
const game = computed(() => props.game)

//data
const toast = useToast()
const store = useLobbyStore()

const { lobby, currentUserId, isLobbyAdmin } = storeToRefs(store)
const { pickPlayer, removePick } = store

const isPickingStarted = ref(false)
const pickingStartingTimer = ref<number>()
const timeUntilPicking = ref<string>()
const pickTime = ref(addMinutes(game.value.dateTime, -30))
const isRosterVisible = ref(false)

//computed
const isStarted = computed(() => game.value.gameState !== GameState.Upcoming)
const isInProgress = computed(() => game.value.gameState === GameState.Live)
const isOver = computed(() => game.value.gameState === GameState.Final)
const isExtraTime = computed(() => game.value.period > 3)
const isThirdPeriod = computed(() => game.value.period === 3)
const isCloseGame = computed(() => Math.abs(game.value.awayTeam.score - game.value.homeTeam.score) <= 1)
const isLateInPeriod = computed(() => game.value.minutesRemainingInPeriod < 5)
const bots = computed(() => lobby.value?.members.filter((m) => m.isBot) ?? [])
const teams = computed(() => ({ home: game.value.homeTeam, away: game.value.awayTeam }))
const pickedPlayers = computed(
  () =>
    lobby.value?.members.reduce((playerIdLookup: { [key: number]: string }, member) => {
      member.picks.forEach((pick) => (playerIdLookup[pick.playerId] = member.name))
      return playerIdLookup
    }, {}) ?? {}
)
const currentUser = computed(() => lobby.value!.members.find((m) => m.userId === currentUserId.value))
const currentUserCanPick = computed(
  () => isPickingStarted.value && (currentUserHasPicksForTeam(game.value.awayTeam) || currentUserHasPicksForTeam(game.value.homeTeam))
)
const pickTimeFormatted = computed(() => format(pickTime.value, 'p'))

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
  return '5-on-5'
  // const situation = game.value.situation
  // if (!situation) return '5-on-5'

  // const homeStrength = situation.homeTeam?.strength ?? 5
  // const awayStrength = situation.awayTeam?.strength ?? 5

  // return homeStrength >= awayStrength ? `${homeStrength}-on-${awayStrength}` : `${awayStrength}-on-${homeStrength}`
})

//hooks
;(async function onCreated() {
  updateTimeUntilPicking()

  if (!isPickingStarted.value) {
    pickingStartingTimer.value = window.setInterval(() => {
      updateTimeUntilPicking()
    }, 1000)
  }
})()

//methods
function updateTimeUntilPicking() {
  isPickingStarted.value = pickTime.value <= new Date()
  if (isPickingStarted.value) return window.clearInterval(pickingStartingTimer.value)

  const duration = intervalToDuration({ start: pickTime.value, end: new Date() })
  const lessThanOneMinute = duration.hours === 0 && duration.minutes == 0
  timeUntilPicking.value = lessThanOneMinute
    ? formatDuration(duration, { format: ['seconds'], zero: true })
    : (timeUntilPicking.value = formatDuration(duration, { format: ['hours', 'minutes'] }))
}

async function getPickIdAndRemovePick(playerId: number) {
  const picks = lobby.value?.members.flatMap((m) => m.picks)
  const pick = picks?.find((p) => p.playerId === playerId)
  if (!pick) return

  await removePick(pick.id)
}

async function pick(playerId: number, teamId: number, lobbyMemberId?: string) {
  if (!lobby.value) return
  lobbyMemberId = lobbyMemberId ?? currentUser.value?.id
  if (!lobbyMemberId) return

  const picks = lobby.value.members.flatMap((m) => m.picks)
  const existingPick = picks.find((p) => p.gameId === game.value.id && p.playerId === playerId)

  if (existingPick) {
    const member = lobby.value.members.find((m) => m.picks.includes(existingPick))
    let name = member ? `<strong>${member.name}</strong> has` : 'Someone has'
    if (member?.userId === currentUserId.value) name = 'You have'
    toast.error(`Oops! ${name} already picked this player.`)
    return
  }

  await pickPlayer(new PickRequest(lobbyMemberId, playerId, game.value.id, teamId))
}

async function makeBotPicks(team: GameTeam) {
  const botsWithPicks = bots.value.filter((bot) => userHasPicksForTeam(bot, team))
  const delayBetweenBotPicks = 1000

  botsWithPicks.forEach((bot, idx) => {
    let waitTime = idx * delayBetweenBotPicks
    const picks = bot.picks
    if (!team.roster || !picks) return false

    const picksMade = picks.filter((p: LobbyMemberPick) => p.gameId === game.value.id && team.roster.some((r) => r.id === p.playerId)).length
    const picksRemaining = lobby.value!.picksPerTeam - picksMade

    for (let i = 0; i < picksRemaining; i++) {
      window.setTimeout(async () => await makeBotPick(bot, team), waitTime)
      waitTime += botsWithPicks.length * delayBetweenBotPicks
    }
  })

  async function makeBotPick(bot: Bot, team: GameTeam) {
    const style = bot.botPickStyle
    const availablePlayers = team.roster.filter((player) => !pickedPlayers.value[player.id])

    const botPickStrategies = {
      [BotPickStyle.Best]: {
        preferredRange: [0, 0],
        fallbackStrategy: BotFallbackPickStrategy.BestAvailable
      },
      [BotPickStyle.Good]: {
        preferredRange: [0, 5],
        fallbackStrategy: BotFallbackPickStrategy.BestBelowRange
      },
      [BotPickStyle.Average]: {
        preferredRange: [5, 10],
        fallbackStrategy: BotFallbackPickStrategy.BestBelowRange
      },
      [BotPickStyle.Bad]: {
        preferredRange: [team.roster.length - 6, team.roster.length - 1],
        fallbackStrategy: BotFallbackPickStrategy.WorstAboveRange
      },
      [BotPickStyle.Worst]: {
        preferredRange: [team.roster.length - 1, team.roster.length - 1],
        fallbackStrategy: BotFallbackPickStrategy.WorstAboveRange
      },
      [BotPickStyle.Random]: {
        preferredRange: [0, team.roster.length - 1],
        fallbackStrategy: BotFallbackPickStrategy.Random
      }
    }

    const [rangeStart, rangeEnd] = botPickStrategies[style].preferredRange
    const fallbackStrategy = botPickStrategies[style].fallbackStrategy

    const preferredPlayers = team.roster.slice(rangeStart, rangeEnd + 1).filter((p) => availablePlayers.includes(p))
    if (preferredPlayers.length) return await pick(preferredPlayers.random().id, team.id, bot.id)

    if (fallbackStrategy === BotFallbackPickStrategy.BestAvailable) return await pick(availablePlayers[0].id, team.id, bot.id)

    if (fallbackStrategy === BotFallbackPickStrategy.BestBelowRange) {
      const availableBelowRange = team.roster.slice(rangeEnd).filter((p) => availablePlayers.includes(p))
      if (availableBelowRange.length) return await pick(availableBelowRange[0].id, team.id, bot.id)
      return await pick(availablePlayers[availablePlayers.length - 1].id, team.id, bot.id)
    }

    if (fallbackStrategy === BotFallbackPickStrategy.WorstAboveRange) {
      const availableAboveRange = team.roster.slice(0, rangeStart + 1)
      if (availableAboveRange.length) return await pick(availableAboveRange[availableAboveRange.length - 1].id, team.id, bot.id)
      return await pick(availablePlayers[0].id, team.id, bot.id)
    }

    if (fallbackStrategy === BotFallbackPickStrategy.WorstAvailable) {
      return await pick(availablePlayers[availablePlayers.length - 1].id, team.id, bot.id)
    }

    if (fallbackStrategy === BotFallbackPickStrategy.Random) {
      return await pick(availablePlayers.random().id, team.id, bot.id)
    }
  }
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

    let output = `${player.firstName[0]} ${player.lastName}`
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

function botsHavePicks(team: GameTeam) {
  return bots.value.some((bot) => userHasPicksForTeam(bot, team))
}

function userHasPicksForTeam(member: LobbyMember, team: GameTeam) {
  if (isOver.value || !team.roster.length) return false

  const currentUserPicks = member.picks
  const count = currentUserPicks.filter(
    (pick) => pick.gameId === game.value.id && team.roster.some((rosterPlayer) => rosterPlayer.id === pick.playerId)
  ).length

  return count < lobby.value!.picksPerTeam
}

function currentUserHasPicksForTeam(team: GameTeam) {
  if (!currentUser.value) return false
  return userHasPicksForTeam(currentUser.value, team)
}

function isCurrentUserPick(player: Player) {
  return !!currentUser.value?.picks.some((p) => p.gameId === game.value.id && p.playerId === player.id)
}

function currentUserCanPickPlayer(player: Player, team: GameTeam) {
  return isPickingStarted.value && !isPlayerPicked(player.id) && currentUserHasPicksForTeam(team)
}

function currentUserCanRemovePick(player: Player) {
  return isLobbyAdmin.value || (isCurrentUserPick(player) && !isStarted.value)
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
                  <span v-if="situation === TeamSituation.PowerPlay">
                    {{ strengthString === '5-on-4' ? 'PP' : strengthString }}
                  </span>
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
              <span v-if="!isPickingStarted" class="text-stone-600 small mt-1 mb-n1 ms-3"
                >Picks open @ <strong>{{ pickTimeFormatted }}</strong></span
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
                        <th colspan="3">
                          <a
                            v-if="botsHavePicks(team) && isLobbyAdmin"
                            @click="makeBotPicks(team)"
                            role="button"
                            style="height: 14px; line-height: 14px"
                            class="text-decoration-none btn btn-primary py-0 fw-bold text-uppercase px-1 fs-8"
                          >
                            Make Bot Picks
                          </a>
                        </th>
                        <th class="text-right" style="width: 40px">GP</th>
                        <th class="text-right" style="width: 40px">G</th>
                        <th class="text-right" style="width: 40px">A</th>
                        <th class="text-right" style="width: 40px">P</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="player in team.roster.filter(p => p.position !== 'G')" :key="player.id">
                        <td class="text-right" style="width: 20px">{{ getFriendlyPosition(player.position) }}</td>
                        <td class="text-right" style="width: 40px">#{{ player.number }}</td>
                        <td>
                          <span class="text-stone-700" :class="{ 'fw-bold': isCurrentUserPick(player) }">{{
                            `${player.firstName} ${player.lastName}`
                          }}</span>
                          <a
                            v-if="currentUserCanPickPlayer(player, team)"
                            role="button"
                            class="btn btn-primary py-0 px-1 text-decoration-none fs-8 fw-bold ms-2"
                            style="height: 12px; line-height: 12px; margin-top: -1px"
                            @click="pick(player.id, team.id)"
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
