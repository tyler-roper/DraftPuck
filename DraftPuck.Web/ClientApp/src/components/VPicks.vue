<script setup lang="ts">
//#region imports
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { computed, ref, watch } from 'vue'
import VPick from '@/components/VPick.vue'
import { addMinutes, differenceInMilliseconds, formatDuration, intervalToDuration } from 'date-fns'
import GamePickableStatus from '@/enums/gamePickableStatus'
import { useToast } from 'vue-toastification'
import PickRequest from '@/models/pickRequest'
import GameState from '@/enums/gameState'
//#endregion

//#region props
const props = defineProps<{
  games: Game[]
  selectedGame?: Game
}>()

const games = computed(() => props.games)
//#endregion

//#region refs
const toast = useToast()
const store = useLobbyStore()

const selectedTeam = ref<GameTeam>()
const teamRosterContainer = ref<HTMLDivElement | null>(null)
const gameCountdowns = ref<Record<number, { asString: string; asMilliseconds: number }>>()
const countdownTimer = ref<number>()
const { lobby, currentUserId, currentSystemTime, isLobbyAdmin } = storeToRefs(store)
const { pickPlayer, removePick } = store
const selectedPlayers = ref<Player[]>([])
const justLockedIn = ref(false)
const selectedMember = ref<LobbyMember>()
const removingPlayer = ref<Player>()
//#endregion

//#region computed
const selectedGame = computed(() => props.selectedGame)
const currentMember = computed(() => lobby.value!.members.find((m) => m.userId === currentUserId.value)!)

const selectedMemberPlayerAndGamePicks = computed(() => {
  const member = selectedMember.value ?? currentMember.value
  return member.picks
    .reduce<{ game: Game; player: Player }[]>((acc, pick) => {
      const game = games.value.find((g) => g.id === pick.gameId)
      if (!game) return acc
      const player = [...game.homeTeam.roster, ...game.awayTeam.roster].find((p) => p.id === pick.playerId)!
      return [...acc, { game, player }]
    }, [])
    .sort((a, b) => {
      const aState = a.game.gameState
      const bState = b.game.gameState
      return (
        Number(bState === GameState.Live) - Number(aState === GameState.Live) ||
        Number(bState === GameState.Upcoming) - Number(aState === GameState.Upcoming)
      )
    })
})

const nextUpcomingPicks = computed(() => {
  if (!gameCountdowns.value) return ''
  const upcomingGames = Object.entries(gameCountdowns.value)
    .filter(([key,value]) => value.asMilliseconds > 0 && lobby.value?.gameIds.includes(Number(key)))
    .map(([_,v]) => v)

  if (upcomingGames.length) return upcomingGames.reduce((prev, curr) => (prev.asMilliseconds < curr.asMilliseconds ? prev : curr)).asString
  return ''
})

const firstPickableGame = computed(() => games.value.find(currentUserHasPicksForGame))
const isSelectedGameLocked = computed(() => !lobby.value?.gameIds.includes(selectedGame.value!.id))
const isSelectedGameStarted = computed(() => selectedGame.value!.gameState !== GameState.Upcoming)
const isSelectedGameOver = computed(() => selectedGame.value!.gameState === GameState.Final)

const gamePickableStatusForCurrentMember = computed(() => {
  if (isSelectedGameLocked.value) return GamePickableStatus.Locked
  if (!isSelectedGameStarted.value && selectedGame.value!.dateTime > addMinutes(currentSystemTime.value, 30)) return GamePickableStatus.Upcoming
  if (isSelectedGameOver.value) return GamePickableStatus.GameComplete

  return currentUserHasPicksForGame(selectedGame.value!) ? GamePickableStatus.PicksAvailable : GamePickableStatus.PicksMade
})

const selectedTeamRosterSorted = computed(() => {
  if (selectedTeam.value === undefined) return []

  return [...selectedTeam.value!.roster].sort((a, b) => {
    const pickedPlayerIds = currentMember.value.picks.map(({ playerId }) => playerId)
    return Number(pickedPlayerIds.includes(b.id)) - Number(pickedPlayerIds.includes(a.id))
  })
})
//#endregion

//#region hooks
;(async function onCreated() {
  getGameCountdowns()
  countdownTimer.value = window.setInterval(getGameCountdowns, 1000)
})()
//#endregion

//#region methods
function getGameCountdowns() {
  gameCountdowns.value = games.value.reduce<Record<number, { asString: string; asMilliseconds: number }>>((acc, game) => {
    const pickTime = addMinutes(game.dateTime, -30)
    const duration = intervalToDuration({ start: pickTime, end: currentSystemTime.value })
    const lessThanOneMinute = duration.hours === 0 && duration.minutes == 0
    acc[game.id] = {
      asString: lessThanOneMinute
        ? formatDuration(duration, { format: ['seconds'], zero: true })
        : formatDuration(duration, { format: ['hours', 'minutes'] }),
      asMilliseconds: differenceInMilliseconds(pickTime, currentSystemTime.value)
    }

    return acc
  }, {})
}

function selectTeam(team: GameTeam) {
  if (team === selectedTeam.value) return
  selectedTeam.value = team
  selectedPlayers.value = []

  if (!teamRosterContainer.value) return
  teamRosterContainer.value.scrollTop = 0
}

async function pick(playerId: number, teamId: number, lobbyMemberId?: string) {
  if (!lobby.value) return
  lobbyMemberId = lobbyMemberId ?? currentMember.value.id
  if (!lobbyMemberId) return

  const picks = lobby.value.members.flatMap((m) => m.picks)
  const existingPick = picks.find((p) => p.gameId === selectedGame.value!.id && p.playerId === playerId)

  if (existingPick) {
    const picker = lobby.value.members.find((lm) => lm.id === lobbyMemberId)
    if (picker != undefined && picker.userId === currentUserId.value) {
      const member = lobby.value.members.find((m) => m.picks.includes(existingPick))
      let name = member ? `<strong>${member.name}</strong> has` : 'Someone has'
      if (member?.userId === currentUserId.value) name = 'You have'
      toast.error(`Oops! ${name} already picked this player.`)
    }
    return
  }

  await pickPlayer(new PickRequest(lobbyMemberId, playerId, selectedGame.value!.id, teamId))
}

async function lockIn() {
  await Promise.all(selectedPlayers.value.map(({ id, teamId }) => pick(id, teamId, currentMember.value.id)))

  selectedPlayers.value = []

  const picksLeftForThisTeam = getPicksRemainingByMemberAndTeam(currentMember.value, selectedTeam.value!)
  if (picksLeftForThisTeam === 0) {
    justLockedIn.value = true
    window.setTimeout(() => (justLockedIn.value = false), 700)

    if (currentUserHasPicksForGame(selectedGame.value!))
      window.setTimeout(
        () => selectTeam(selectedGame.value!.awayTeam.id === selectedTeam.value!.id ? selectedGame.value!.homeTeam : selectedGame.value!.awayTeam),
        1000
      )
    else if (firstPickableGame.value !== undefined) window.setTimeout(() => selectGame(firstPickableGame.value), 1000)
    else {
      window.setTimeout(() => {
        selectGame(undefined)
        toast.success("You've finished making all of your picks (for now).")
      }, 1000)
    }
  }
}

function checkIfSelectedPlayerWasPicked() {
  if (!lobby.value) return

  const allPickedPlayerIds = lobby.value.members.flatMap((m) => m.picks.map((p) => p.playerId))
  const oldSelectedPlayersCount = selectedPlayers.value.length
  selectedPlayers.value = selectedPlayers.value.filter((player) => !allPickedPlayerIds.includes(player.id))
  const newSelectedPlayersCount = selectedPlayers.value.length

  if (oldSelectedPlayersCount > newSelectedPlayersCount) toast.error('Sorry, someone picked one of your selections!')
}

function selectMember(member?: LobbyMember) {
  if (member === undefined || member.id === currentMember.value.id) {
    selectedMember.value = undefined
    return
  }

  selectedMember.value = member
}

async function unpick(game: Game, player: Player) {
  if (!canRemovePlayer(game, player)) return
  const pick = lobby.value!.members.flatMap((m) => m.picks).find((p) => p.playerId === player.id)
  await removePick(pick!.id)
}
//#endregion

//#region helpers
function canRemovePlayer(game: Game, player: Player) {
  //not picked
  const pick = lobby.value?.members.flatMap((m) => m.picks).find((p) => p.playerId === player.id)
  if (!pick) return false

  if (isLobbyAdmin.value) return true

  //game started
  if (game.gameState !== GameState.Upcoming) return false

  //not current member's pick
  if (pick.lobbyMemberId !== currentMember.value.id) return false

  return true
}

function isRemovingPlayer(player: Player) {
  return removingPlayer.value && removingPlayer.value.id === player.id
}

function isPlayerSelected(player: Player) {
  return selectedPlayers.value.includes(player)
}

function currentUserHasPicksForGame(game: Game) {
  const isGameStartingWithin30Minutes = game.gameState === GameState.Upcoming && addMinutes(game.dateTime, -30) < currentSystemTime.value
  const isGamePickable = game.gameState === GameState.Live || isGameStartingWithin30Minutes

  const picksPerTeam = lobby.value!.picksPerTeam
  const picksAlreadyMadeForGame = currentMember.value.picks.filter((pick) => pick.gameId === game.id).length
  return isGamePickable && picksAlreadyMadeForGame < picksPerTeam * 2
}

function togglePlayerSelection(player: Player) {
  if (!isPlayerSelected(player)) return selectedPlayers.value.push(player)
  selectedPlayers.value = selectedPlayers.value.filter((p) => p !== player)
}

function togglePlayerRemoving(game: Game, player: Player) {
  if (!canRemovePlayer(game, player)) return
  if (!isRemovingPlayer(player)) return (removingPlayer.value = player)
  removingPlayer.value = undefined
}

function getPicksRemainingByMemberAndTeam(member: LobbyMember, team: GameTeam) {
  const memberPicks = member.picks
  return (
    lobby.value!.picksPerTeam -
    memberPicks.filter((pick) => team.roster.some((rosterPlayer) => rosterPlayer.id === pick.playerId)).length -
    selectedPlayers.value.length
  )
}

function getLogo(team: GameTeam) {
  return `/img/logos/${team.abbreviation}.png`
}

function getLightLogo(team: GameTeam) {
  if (team.abbreviation !== 'TBL') return getLogo(team)
  return `/img/logos/${team.abbreviation}_LIGHT.png`
}
//#endregion

//#region watchers
watch(lobby, async (newValue, oldValue) => {
  if (!newValue || !oldValue) return
  const oldPickCount = oldValue.members.reduce((count, member) => count + member.picks.length, 0)
  const newPickCount = newValue.members.reduce((count, member) => count + member.picks.length, 0)

  if (oldPickCount !== newPickCount) checkIfSelectedPlayerWasPicked()
})

watch(selectedGame, async (newGame, oldGame) => {
  if (newGame === undefined) {
    selectedTeam.value = undefined
    selectedPlayers.value = []
    return
  }

  if (oldGame?.id === newGame.id) return
  selectedTeam.value =
    newGame.homeTeam.roster?.length && getPicksRemainingByMemberAndTeam(currentMember.value, newGame.awayTeam) > 0
      ? newGame.awayTeam
      : newGame.homeTeam
  selectedPlayers.value = []

  if (!teamRosterContainer.value) return
  teamRosterContainer.value.scrollTop = 0
})
//#endregion

//#region emits
const emit = defineEmits(['selectGame'])
function selectGame(game?: Game) {
  emit('selectGame', game)
}
//#endregion
</script>

<template>
  <!-- SCORES RIBBON -->
  <div style="overflow: hidden" class="d-flex flex-column">
    <!-- MY PICKS -->
    <template v-if="selectedGame === undefined">
      <div>
        <div
          v-if="currentMember.picks.length > 0"
          class="p-2 fs-5 fw-bolder text-stone-0 bg-stone-900 d-flex justify-content-between align-items-center"
        >
          <div class="d-block dropdown">
            <a role="button" class="text-white text-decoration-none" data-bs-toggle="dropdown">
              <span v-if="selectedMember === undefined" class="d-block fs-7">MY</span>
              <span v-if="selectedMember !== undefined" class="d-block fs-7">{{ selectedMember.name }}&apos;s</span>
              <span class="d-block mt-n2">PICKS<i class="fi fi-sr-caret-down fs-3 position-relative" style="top: 4px"></i></span>
            </a>
            <div class="dropdown-menu">
              <a
                v-for="member in lobby!.members.sort((a, b) => Number(b.id === currentMember.id) - Number(a.id === currentMember.id))"
                :key="member.id"
                class="dropdown-item py-2"
                role="button"
                @click="selectMember(member)"
              >
                <span class="position-relative" style="top: 2px">
                  <i v-if="member.id === currentMember.id" class="fi fi-sr-user me-2 text-primary"></i>
                  <i v-else-if="member.isBot" class="fi fi-sr-user-robot me-2 text-stone-400"></i>
                  <i v-else class="fi fi-sr-user me-2 text-blue"></i>
                </span>
                <span class="ms-1" :class="{ 'text-primary': member.id === currentMember.id, 'fw-bold': member.id === selectedMember?.id }">{{
                  member.id === currentMember.id ? 'Me' : member.name
                }}</span>
              </a>
            </div>
          </div>
          <a
            v-if="firstPickableGame !== undefined"
            @click="selectGame(firstPickableGame)"
            role="button"
            class="fs-6 text-primary text-decoration-none"
            >Make Picks Now
          </a>
          <div
            v-if="selectedMember === undefined && firstPickableGame === undefined"
            class="ms-auto fs-7 text-stone-0 fw-normal text-decoration-none"
          >
            <span class="d-block text-end"
              ><i class="text-success fi fi-sr-check-circle position-relative pe-2" style="top: 2px"></i>All Picks Made</span
            >
            <span v-if="nextUpcomingPicks !== ''" class="fs-8 text-stone-300 fw-normal text-decoration-none d-block">
              (More available in {{ nextUpcomingPicks }})
            </span>
          </div>
        </div>
      </div>
      <div class="flex-grow-1" style="overflow-y: scroll">
        <VPick
          v-for="{ game, player } in selectedMemberPlayerAndGamePicks"
          :key="player.id"
          :player="player"
          :game="game"
          :selected-player-count="0"
          :is-removing="isRemovingPlayer(player)"
          @click="togglePlayerRemoving(game, player)"
          @on-unpicked="unpick(game, player)"
        />

        <template v-if="currentMember.picks.length === 0">
          <span class="text-center p-5 fs-1 text-stone-000 d-block text-uppercase"
            >Welcome to<br /><span class="d-inline-block mt-n3 text-primary fw-bold" style="font-size: 50px">Draftpuck</span></span
          >
          <div class="text-center p-4" v-if="firstPickableGame !== undefined">
            <span class="fs-4 text-stone-0 fw-bold d-block mb-2">What are you waiting for?</span>
            <button @click="selectGame(firstPickableGame)" class="py-2 px-4 gradient-button fw-bold fs-1 h-100 text-uppercase emphasized">
              Make Picks Now
            </button>
          </div>
          <div v-else-if="nextUpcomingPicks !== ''" class="text-center p-4">
            <div class="p-3 bg-stone-900 rounded">
              <i class="text-stone-400 fi fi-br-alarm-clock d-block mb-n2" style="font-size: 50px"></i>
              <span class="fs-4 text-stone-400 fw-bold d-block">Next picks in</span>
              <span class="fs-1 text-stone-0 fw-bold d-block mt-n1">{{ nextUpcomingPicks }}</span>
            </div>
            <div class="mt-5">
              <span class="fs-5 text-primary d-block fw-bold">Need a reminder?</span>
              <span class="fs-6 text-stone-300 d-block px-5"
                >Click the <i class="fs-4 d-inline-block text-stone-0 fi fi-rr-settings mx-1" style="position: relative; top: 5px"></i> at the top of
                the page to change notification settings.</span
              >
            </div>
          </div>
          <div v-else class="text-center p-4">
            <div class="p-3 bg-stone-900 rounded">
              <i class="text-stone-400 fi fi-rr-sad d-block mb-n2" style="font-size: 50px"></i>
              <span class="fs-4 text-stone-400 fw-bold d-block">Sorry, no more picks today.</span>
            </div>
          </div>
        </template>
      </div>
    </template>

    <!-- GAME SELECTED -->
    <template v-if="selectedGame !== undefined">
      <!-- BREADCRUMB -->
      <div class="p-2 top-breadcrumb bg-stone-900 d-flex justify-content-between">
        <a role="button" @click="selectGame()" class="fw-bold text-primary text-decoration-none d-block"
          ><i class="fi fi-sr-caret-left"></i>My Picks</a
        >
      </div>

      <!-- CONTENT BOX -->
      <div class="fs-5 d-flex justify-content-between">
        <!-- TEAM HEADERS -->
        <!-- AWAY -->
        <div
          class="p-2 d-flex align-items-center w-50"
          @click="selectTeam(selectedGame.awayTeam)"
          :class="{ 'o-75': selectedTeam !== selectedGame.awayTeam, 'bg-stone-700': selectedTeam === selectedGame.awayTeam }"
        >
          <div>
            <img style="height: 25px" :src="getLightLogo(selectedGame.awayTeam)" class="d-block" />
          </div>
          <div class="text-uppercase fs-6">
            <div v-if="selectedGame.awayTeam.location.length > 1" class="fs-7 text-stone-300">{{ selectedGame.awayTeam.location }}</div>
            <div class="fw-bold" :class="{ 'mt-n2': selectedGame.awayTeam.location.length > 1 }">{{ selectedGame.awayTeam.name }}</div>
          </div>
        </div>

        <!-- HOME -->
        <div
          class="p-2 d-flex align-items-center text-end w-50 justify-content-end"
          @click="selectTeam(selectedGame.homeTeam)"
          :class="{ 'o-75': selectedTeam !== selectedGame.homeTeam, 'bg-stone-700': selectedTeam === selectedGame.homeTeam }"
        >
          <div class="text-uppercase fs-6">
            <div v-if="selectedGame.homeTeam.location.length > 1" class="fs-7 text-stone-300">{{ selectedGame.homeTeam.location }}</div>
            <div class="fw-bold" :class="{ 'mt-n2': selectedGame.homeTeam.location.length > 1 }">{{ selectedGame.homeTeam.name }}</div>
          </div>
          <div>
            <img style="height: 25px" :src="getLightLogo(selectedGame.homeTeam)" class="d-block" />
          </div>
        </div>
      </div>

      <!-- ROSTER -->
      <div v-if="selectedTeam?.roster.length" ref="teamRosterContainer" class="flex-grow-1" style="overflow-y: scroll">
        <VPick
          v-for="player in selectedTeamRosterSorted"
          :key="player.id"
          :player="player"
          :game="selectedGame"
          :is-for-picking="true"
          :is-selected="isPlayerSelected(player)"
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

      <!-- LOCK IN -->
      <div
        class="lock-in shadow"
        :class="{ 'just-locked-in': justLockedIn }"
        v-for="(picksRemaining, idx) in [getPicksRemainingByMemberAndTeam(currentMember, selectedTeam!)]"
        :key="idx"
      >
        <template v-if="gamePickableStatusForCurrentMember === GamePickableStatus.Upcoming">
          <div class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-300 d-flex justify-content-center align-items-center">
            <span class="text-stone-600">Picks in</span>
            <i class="fi fi-sr-alarm-clock d-block fs-1 me-2 ms-4 mb-n1 pt-1"></i>
            <span class="d-block fw-bold text-uppercase">{{ gameCountdowns![selectedGame.id].asString }}</span>
          </div>
        </template>
        <template v-else-if="gamePickableStatusForCurrentMember === GamePickableStatus.GameComplete">
          <div class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-600 d-flex justify-content-center align-items-center">
            <i class="fi fi-sr-time-check d-block fs-1 me-2 p-relative mb-n1 mt-1"></i>
            <span class="d-block fw-bold text-uppercase">Game Over</span>
          </div>
        </template>
        <template v-else-if="gamePickableStatusForCurrentMember === GamePickableStatus.Locked">
          <div class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-stone-600 d-flex justify-content-center align-items-center">
            <i class="fi fi-sr-lock d-block fs-1 me-2"></i>
            <span class="d-block fw-bold text-uppercase">Game Locked</span>
          </div>
        </template>
        <template v-else>
          <div
            v-if="picksRemaining <= 0 && selectedPlayers.length === 0"
            class="text-center fs-4 flex-grow-1 py-2 text-uppercase text-primary d-flex justify-content-center align-items-center"
          >
            <i class="fi fi-sr-lock d-block fs-1 me-2"></i>
            <span class="d-block fw-bold text-uppercase">Locked In!</span>
          </div>
          <div
            v-if="picksRemaining > 0 || (picksRemaining === 0 && selectedPlayers.length > 0)"
            class="text-uppercase d-flex align-items-center px-2"
          >
            <div class="fs-4">Picks Remaining:</div>
            <div class="py-2 ms-2 fs-1 fw-bold d-flex align-items-center">
              <img style="width: 33px; height: 37px" :src="getLightLogo(selectedTeam!)" />
              <div>{{ picksRemaining }}</div>
            </div>
          </div>
          <div v-if="picksRemaining > 0 || (picksRemaining === 0 && selectedPlayers.length > 0)">
            <button
              class="btn btn-primary fw-bold fs-3 h-100 rounded-0 text-uppercase"
              :class="{ emphasized: !!selectedPlayers.length }"
              :disabled="!selectedPlayers.length"
              @click="lockIn()"
            >
              Lock In
            </button>
          </div>
        </template>
      </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.top-breadcrumb {
  border-bottom: 1px solid map-get($custom-colors, 'stone-600');
}

.lock-in {
  background: map-get($custom-colors, 'stone-900');
  display: flex;
  justify-content: space-between;
}

.lock-in.just-locked-in {
  animation: puff-in-center 0.7s cubic-bezier(1, 0, 0, 1) both;
}

.lock-in button,
.gradient-button {
  transition: 0.3s;
  background: linear-gradient(map-get($custom-colors, 'amber-300'), map-get($custom-colors, 'primary'));
  transform: translateY(0);
  border: none;
  color: map-get($custom-colors, 'stone-800');
}

.lock-in button.emphasized,
.gradient-button.emphasized {
  animation: pulsate 2s ease-in-out infinite;
  position: relative;
  text-shadow: 1px 1px 0 rgba(255, 255, 255, 0.7);
}

@keyframes pulsate {
  0% {
    box-shadow: 0 0 3px map-get($custom-colors, 'amber-300');
    transform: translateY(0);
  }
  50% {
    box-shadow: 0 0 10px map-get($custom-colors, 'amber-300');
    transform: translateY(-6px);
  }
  100% {
    box-shadow: 0 0 3px map-get($custom-colors, 'amber-300');
    transform: translateY(0);
  }
}

@keyframes puff-in-center {
  0% {
    transform: scale(5) translateY(-100px);
    filter: blur(4px);
    opacity: 0;
  }
  100% {
    transform: scale(1);
    filter: blur(0px);
    opacity: 1;
  }
}
</style>

<style lang="scss">
@import '@/assets/scss/custom-colors.scss';
</style>
