<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useToast } from '@/composables/useToast'
import { useLobbyStore } from '@/stores/lobby'
import { useSystemStore } from '@/stores/system'
import { canPickPlayer, canRemovePick, findPick } from '@/helpers/pickRules'
import { friendlyPosition, pickerNames } from '@/helpers/scoreboard'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { lightTeamLogoUrl } from '@/helpers/teamLogos'
import TeamColors from '@/models/teamColorLookup'
import PickRequest from '@/models/pickRequest'

/** Both teams' skaters with their season stats, who picked each, and Pick / remove buttons for the viewer. */
const props = defineProps<{
  game: Game
}>()

const toast = useToast()
const lobbyStore = useLobbyStore()
const { lobby, currentMember } = storeToRefs(lobbyStore)
const { currentSystemTime } = storeToRefs(useSystemStore())

// Players sent to the server, until it answers.
const pendingPlayerIds = ref(new Set<number>())

const teams = computed(() => [props.game.homeTeam, props.game.awayTeam])
const pickers = computed(() => (lobby.value ? pickerNames(lobby.value, props.game.id) : new Map<number, string>()))
const isMyPick = (player: Player) => !!currentMember.value?.picks.some((p) => p.gameId === props.game.id && p.playerId === player.id)

function canPick(player: Player, team: GameTeam) {
  return (
    !!lobby.value &&
    !!currentMember.value &&
    !pendingPlayerIds.value.has(player.id) &&
    canPickPlayer(lobby.value, currentMember.value, props.game, { id: player.id, teamId: team.id }, currentSystemTime.value)
  )
}

function canRemove(player: Player) {
  const found = lobby.value && findPick(lobby.value, props.game.id, player.id)
  return !!found && !!currentMember.value && canRemovePick(lobby.value!, props.game, found.pick, currentMember.value)
}

/** Picks a player for the viewer. Pending until the server answers; a refusal shows the server's reason. */
async function pick(player: Player, team: GameTeam) {
  if (!currentMember.value || pendingPlayerIds.value.has(player.id)) return

  pendingPlayerIds.value = new Set([...pendingPlayerIds.value, player.id])
  const [{ error }] = await lobbyStore.makePicks([new PickRequest(currentMember.value.id, player.id, props.game.id, team.id)])
  pendingPlayerIds.value = new Set([...pendingPlayerIds.value].filter((id) => id !== player.id))

  if (error) toast.error(`${player.firstName} ${player.lastName}: ${error}`)
}

async function removePick(player: Player) {
  const found = lobby.value && findPick(lobby.value, props.game.id, player.id)
  if (!found) return

  try {
    await lobbyStore.removePick(found.pick.id)
  } catch (error) {
    toast.error(getErrorMessage(error, "Couldn't remove that pick. Please try again."))
  }
}
</script>

<template>
  <div class="row inset-shadow">
    <div v-for="team in teams" :key="team.id" class="roster-split col-lg-6 col-12">
      <div class="px-3 py-2 fw-bold text-stone-0 d-flex align-items-center" :style="{ 'background-color': TeamColors[team.id] }">
        <img class="d-block" :src="lightTeamLogoUrl(team)" style="width: 40px; height: 40px" />
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
            <td class="text-right" style="width: 20px">{{ friendlyPosition(player.position) }}</td>
            <td class="text-right" style="width: 40px">#{{ player.number }}</td>
            <td>
              <span class="text-stone-700" :class="{ 'fw-bold': isMyPick(player) }">{{ `${player.firstName} ${player.lastName}` }}</span>
              <a
                v-if="canPick(player, team)"
                role="button"
                class="btn btn-primary py-0 px-1 text-decoration-none fs-8 fw-bold ms-2"
                style="height: 12px; line-height: 12px; margin-top: -1px"
                @click="pick(player, team)"
              >
                PICK
              </a>

              <span v-if="pickers.has(player.id)" class="badge text-upperase ms-2" :class="isMyPick(player) ? 'bg-danger' : 'bg-blue'">
                {{ isMyPick(player) ? '(You)' : pickers.get(player.id) }}
                <a v-if="canRemove(player)" role="button" class="ms-1 text-white" @click="removePick(player)">x</a>
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
</style>
