<script setup lang="ts">
import { computed } from 'vue'
import GameState from '@/enums/gameState'
import TeamSituation from '@/enums/teamSituation'
import TeamColors from '@/models/teamColorLookup'
import { formatGameClock } from '@/helpers/gameHelpers'
import { goalsInPeriod, linescoreColumns, powerPlayLabel, scorerSummary } from '@/helpers/scoreboard'
import { lightTeamLogoUrl, teamLogoUrl } from '@/helpers/teamLogos'

/*
 * The scoreboard's header and team rows: the clock, goals by period, the score and the scorers. Rendered as the
 * <thead> and <tbody> of VGameScoreboard's table, which the site's score-table styles lay out.
 */

const props = defineProps<{
  game: Game
}>()

const isStarted = computed(() => props.game.gameState !== GameState.Upcoming)
const isOver = computed(() => props.game.gameState === GameState.Final)
const columns = computed(() => linescoreColumns(props.game))
// Home first.
const teams = computed(() => [props.game.homeTeam, props.game.awayTeam])

const opponent = (team: GameTeam) => (team.id === props.game.homeTeam.id ? props.game.awayTeam : props.game.homeTeam)
const won = (team: GameTeam) => isOver.value && team.score > opponent(team).score
// Greyed out before the game, and for the loser after it.
const isGreyedOut = (team: GameTeam) => !isStarted.value || (isOver.value && team.score < opponent(team).score)
</script>

<template>
  <thead>
    <tr>
      <th style="width: 290px">
        <span class="fw-bold">{{ formatGameClock(game) }}</span>
      </th>
      <th v-for="column in columns" :key="column.number" style="width: 45px">
        <span v-if="isStarted" class="fw-bold">{{ column.label }}</span>
      </th>
      <th style="width: 45px"></th>
      <th></th>
    </tr>
  </thead>

  <tbody>
    <tr v-for="team in teams" :key="team.id">
      <td
        class="bg-stone-150"
        :class="{ 'text-stone-0': won(team) }"
        :style="{ 'background-color': won(team) ? `${TeamColors[team.id]} !important` : '' }"
      >
        <div class="d-flex align-items-center">
          <img
            :src="won(team) ? lightTeamLogoUrl(team) : teamLogoUrl(team)"
            style="width: 50px; height: 50px"
            class="d-block"
            :style="{ filter: isGreyedOut(team) ? 'grayscale(1)' : '' }"
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
              <span v-if="situation === TeamSituation.PowerPlay">{{ powerPlayLabel(game) }}</span>
              <span v-if="situation === TeamSituation.EmptyNet">EN</span>
            </span>
          </div>
        </div>
      </td>

      <td v-for="column in columns" :key="column.number">
        <span v-if="isStarted && column.isReached">{{ goalsInPeriod(game, team, column.number) }}</span>
      </td>

      <td>
        <span v-if="isStarted" class="fw-bold fs-6">{{ team.score }}</span>
      </td>

      <td class="ps-4">{{ scorerSummary(game, team) }}</td>
    </tr>
  </tbody>
</template>
