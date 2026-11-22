<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import VPick from '@/components/VPick.vue'
import VMyPicks from '@/components/VMyPicks.vue'
import VPickTeamTabs from '@/components/VPickTeamTabs.vue'
import VLockInBar from '@/components/VLockInBar.vue'
import { useToast } from '@/composables/useToast'
import { useLobbyStore } from '@/stores/lobby'
import { useSystemStore } from '@/stores/system'
import { findPick, getPickableStatus, picksRemaining } from '@/helpers/pickRules'
import { firstTeamToPick, nextPickStep, skatersForPicking } from '@/helpers/picksPanel'
import { formatCountdown } from '@/helpers/dateHelpers'
import PickRequest from '@/models/pickRequest'

/*
 * The phone's picks panel: the member's picks (VMyPicks) until a game is selected, then that game's rosters to select
 * players from and lock them in.
 */

const props = defineProps<{
  games: Game[]
  selectedGame?: Game
}>()

const emit = defineEmits<{ selectGame: [game?: Game] }>()

const toast = useToast()
const lobbyStore = useLobbyStore()
const { lobby, currentMember } = storeToRefs(lobbyStore)
const { currentSystemTime } = storeToRefs(useSystemStore())

const selectedTeamId = ref<number>()
const rosterContainer = ref<HTMLDivElement | null>(null)
// Players selected and not locked in yet.
const selectedPlayers = ref<Player[]>([])
// Selected players sent to the server by Lock In, until it answers.
const pendingPlayerIds = ref(new Set<number>())
const justLockedIn = ref(false)

const game = computed(() => props.selectedGame)
const member = computed(() => currentMember.value!)
const selectedTeam = computed(() => (selectedTeamId.value === game.value?.homeTeam.id ? game.value?.homeTeam : game.value?.awayTeam))
const roster = computed(() => (selectedTeam.value ? skatersForPicking(selectedTeam.value, member.value) : []))
const status = computed(() => getPickableStatus(lobby.value!, member.value, game.value!, currentSystemTime.value))
const picksLeftForTeam = computed(() =>
  picksRemaining(lobby.value!, member.value, game.value!.id, selectedTeam.value!.id, selectedPlayers.value.length)
)

function selectTeam(team: GameTeam) {
  if (team === selectedTeam.value) return
  selectedTeamId.value = team.id
  selectedPlayers.value = []
  if (rosterContainer.value) rosterContainer.value.scrollTop = 0
}

function togglePlayerSelection(player: Player) {
  if (selectedPlayers.value.includes(player)) selectedPlayers.value = selectedPlayers.value.filter((p) => p !== player)
  else selectedPlayers.value.push(player)
}

/**
 * Sends the selected players to the server. They show as pending until it answers; accepted ones become picks, and
 * refused ones go back to unselected with the server's reason. Moves on to the next team or game only if all succeeded.
 */
async function lockIn() {
  const lockedGame = game.value
  const team = selectedTeam.value
  if (!lockedGame || !team || pendingPlayerIds.value.size > 0 || selectedPlayers.value.length === 0) return

  const players = [...selectedPlayers.value]
  pendingPlayerIds.value = new Set(players.map((p) => p.id))
  const results = await lobbyStore.makePicks(players.map((p) => new PickRequest(member.value.id, p.id, lockedGame.id, p.teamId)))
  pendingPlayerIds.value = new Set()
  selectedPlayers.value = []

  const refused = results.filter((r) => r.error)
  for (const { request, error } of refused) {
    const player = players.find((p) => p.id === request.playerId)!
    toast.error(`${player.firstName} ${player.lastName}: ${error}`)
  }
  if (refused.length > 0 || picksRemaining(lobby.value!, member.value, lockedGame.id, team.id) > 0) return

  justLockedIn.value = true
  window.setTimeout(() => (justLockedIn.value = false), 700)

  const next = nextPickStep(lobby.value!, member.value, lockedGame, team, props.games, currentSystemTime.value)
  window.setTimeout(() => {
    if (next === 'done') {
      emit('selectGame', undefined)
      toast.success("You've finished making all of your picks (for now).", { timeout: 2000 })
    } else if ('team' in next) selectTeam(next.team)
    else emit('selectGame', next.game)
  }, 1000)
}

// A refetched lobby with new picks: unselect any player someone else picked meanwhile. (Only a refetch replaces the
// lobby; this member's own lock-in adds its picks in place, while they're still selected.)
watch(lobby, (newLobby, oldLobby) => {
  if (!newLobby || !oldLobby || !game.value) return
  const pickCount = (l: Lobby) => l.members.reduce((count, m) => count + m.picks.length, 0)
  if (pickCount(newLobby) === pickCount(oldLobby)) return

  const gameId = game.value.id
  const stillFree = selectedPlayers.value.filter((player) => !findPick(newLobby, gameId, player.id))
  if (stillFree.length === selectedPlayers.value.length) return

  selectedPlayers.value = stillFree
  toast.error('Sorry, someone picked one of your selections!')
})

watch(game, (newGame, oldGame) => {
  if (!newGame) {
    selectedTeamId.value = undefined
    selectedPlayers.value = []
    return
  }
  if (oldGame?.id === newGame.id) return

  selectedPlayers.value = []
  selectedTeamId.value = firstTeamToPick(lobby.value!, member.value, newGame).id
  if (rosterContainer.value) rosterContainer.value.scrollTop = 0
})
</script>

<template>
  <div style="overflow: hidden" class="d-flex flex-column">
    <VMyPicks v-if="!game" :games="games" :member="member" @select-game="(g) => emit('selectGame', g)" />

    <template v-else>
      <div class="p-2 top-breadcrumb bg-stone-900 d-flex justify-content-between">
        <a role="button" @click="emit('selectGame', undefined)" class="fw-bold text-primary text-decoration-none d-block"
          ><i class="fi fi-sr-caret-left"></i>My Picks</a
        >
      </div>

      <VPickTeamTabs :game="game" :selected-team-id="selectedTeam?.id" @select="selectTeam" />

      <div v-if="selectedTeam?.roster.length" ref="rosterContainer" class="flex-grow-1" style="overflow-y: scroll">
        <VPick
          v-for="player in roster"
          :key="player.id"
          :player="player"
          :game="game"
          :is-for-picking="true"
          :is-selected="selectedPlayers.includes(player)"
          :is-pending="pendingPlayerIds.has(player.id)"
          :selected-player-count="selectedPlayers.length"
          @on-selected="togglePlayerSelection"
        />
      </div>
      <div
        v-else
        class="bg-stone-900 flex-grow-1 d-flex align-items-center justify-content-center text-center p-5 fw-bold text-stone-700"
        style="font-size: 40px"
      >
        <span class="d-block">
          <i class="fi fi-sr-user-time" style="font-size: 100px"></i>
          <span class="d-block mt-n4">No Rosters Yet</span>
        </span>
      </div>

      <VLockInBar
        v-if="selectedTeam"
        :status="status"
        :team="selectedTeam"
        :picks-remaining="picksLeftForTeam"
        :selected-count="selectedPlayers.length"
        :countdown="formatCountdown(game.pickingOpensUtc, currentSystemTime)"
        :is-pending="pendingPlayerIds.size > 0"
        :just-locked-in="justLockedIn"
        @lock-in="lockIn"
      />
    </template>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.top-breadcrumb {
  border-bottom: 1px solid map-get($custom-colors, 'stone-600');
}
</style>
