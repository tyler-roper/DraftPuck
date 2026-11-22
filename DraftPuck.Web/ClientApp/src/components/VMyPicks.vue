<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import VPick from '@/components/VPick.vue'
import { useToast } from '@/composables/useToast'
import { useLobbyStore } from '@/stores/lobby'
import { useSystemStore } from '@/stores/system'
import { canRemovePick, findPick, hasPicksLeftInGame } from '@/helpers/pickRules'
import { memberPicksWithPlayers, membersViewerFirst, nextPicksCountdown } from '@/helpers/picksPanel'
import { getErrorMessage } from '@/helpers/requestHelpers'

const props = defineProps<{
  games: Game[]
  // The viewer.
  member: LobbyMember
}>()

const emit = defineEmits<{ selectGame: [game: Game] }>()

const toast = useToast()
const lobbyStore = useLobbyStore()
const { lobby } = storeToRefs(lobbyStore)
const { currentSystemTime } = storeToRefs(useSystemStore())

// Whose picks are shown: another member's, or (undefined) the viewer's own.
const shownMember = ref<LobbyMember>()
// The pick showing its remove button.
const removingPlayer = ref<Player>()

const members = computed(() => membersViewerFirst(lobby.value!.members, props.member.id))
const picks = computed(() => memberPicksWithPlayers(shownMember.value ?? props.member, props.games))
const firstPickableGame = computed(() => props.games.find((g) => hasPicksLeftInGame(lobby.value!, props.member, g, currentSystemTime.value)))
const nextPicksIn = computed(() => nextPicksCountdown(lobby.value!, props.games, currentSystemTime.value))

function showMember(member: LobbyMember) {
  shownMember.value = member.id === props.member.id ? undefined : member
}

function canRemove(game: Game, player: Player) {
  const found = findPick(lobby.value!, game.id, player.id)
  return !!found && canRemovePick(lobby.value!, game, found.pick, props.member)
}

function toggleRemoving(game: Game, player: Player) {
  if (!canRemove(game, player)) return
  removingPlayer.value = removingPlayer.value?.id === player.id ? undefined : player
}

async function unpick(game: Game, player: Player) {
  const found = findPick(lobby.value!, game.id, player.id)
  if (!found || !canRemove(game, player)) return
  try {
    await lobbyStore.removePick(found.pick.id)
  } catch (error) {
    toast.error(getErrorMessage(error, "Couldn't remove that pick. Please try again."))
  }
}
</script>

<template>
  <div>
    <div v-if="member.picks.length > 0" class="p-2 fs-5 fw-bolder text-stone-0 bg-stone-900 d-flex justify-content-between align-items-center">
      <div class="d-block dropdown">
        <a role="button" class="text-white text-decoration-none" data-bs-toggle="dropdown">
          <span v-if="shownMember === undefined" class="d-block fs-7">MY</span>
          <span v-else class="d-block fs-7">{{ shownMember.name }}&apos;s</span>
          <span class="d-block mt-n2">PICKS<i class="fi fi-sr-caret-down fs-3 position-relative" style="top: 4px"></i></span>
        </a>
        <div class="dropdown-menu">
          <a v-for="m in members" :key="m.id" class="dropdown-item py-2" role="button" @click="showMember(m)">
            <span class="position-relative" style="top: 2px">
              <i v-if="m.id === member.id" class="fi fi-sr-user me-2 text-primary"></i>
              <i v-else-if="m.isBot" class="fi fi-sr-user-robot me-2 text-stone-400"></i>
              <i v-else class="fi fi-sr-user me-2 text-blue"></i>
            </span>
            <span class="ms-1" :class="{ 'text-primary': m.id === member.id, 'fw-bold': m.id === shownMember?.id }">{{
              m.id === member.id ? 'Me' : m.name
            }}</span>
          </a>
        </div>
      </div>
      <a v-if="firstPickableGame" @click="emit('selectGame', firstPickableGame)" role="button" class="fs-6 text-primary text-decoration-none"
        >Make Picks Now
      </a>
      <div v-if="shownMember === undefined && !firstPickableGame" class="ms-auto fs-7 text-stone-0 fw-normal text-decoration-none">
        <span class="d-block text-end"><i class="text-success fi fi-sr-check-circle position-relative pe-2" style="top: 2px"></i>All Picks Made</span>
        <span v-if="nextPicksIn" class="fs-8 text-stone-300 fw-normal text-decoration-none d-block"> (More available in {{ nextPicksIn }}) </span>
      </div>
    </div>
  </div>
  <div class="flex-grow-1" style="overflow-y: scroll">
    <VPick
      v-for="{ game, player } in picks"
      :key="player.id"
      :player="player"
      :game="game"
      :selected-player-count="0"
      :is-removing="removingPlayer?.id === player.id"
      @click="toggleRemoving(game, player)"
      @on-unpicked="unpick(game, player)"
    />

    <template v-if="member.picks.length === 0">
      <span class="text-center p-5 fs-1 text-stone-000 d-block text-uppercase"
        >Welcome to<br /><span class="d-inline-block mt-n3 text-primary fw-bold" style="font-size: 50px">Draftpuck</span></span
      >
      <div class="text-center p-4" v-if="firstPickableGame">
        <span class="fs-4 text-stone-0 fw-bold d-block mb-2">What are you waiting for?</span>
        <button @click="emit('selectGame', firstPickableGame)" class="py-2 px-4 gradient-button fw-bold fs-1 h-100 text-uppercase emphasized">
          Make Picks Now
        </button>
      </div>
      <div v-else-if="nextPicksIn" class="text-center p-4">
        <div class="p-3 bg-stone-900 rounded">
          <i class="text-stone-400 fi fi-br-alarm-clock d-block mb-n2" style="font-size: 50px"></i>
          <span class="fs-4 text-stone-400 fw-bold d-block">Next picks in</span>
          <span class="fs-1 text-stone-0 fw-bold d-block mt-n1">{{ nextPicksIn }}</span>
        </div>
        <div class="mt-5">
          <span class="fs-5 text-primary d-block fw-bold">Need a reminder?</span>
          <span class="fs-6 text-stone-300 d-block px-5"
            >Click the <i class="fs-4 d-inline-block text-stone-0 fi fi-rr-bell mx-1" style="position: relative; top: 5px"></i> at the top of the page
            to change notification settings.</span
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

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.gradient-button {
  transition: 0.3s;
  background: linear-gradient(map-get($custom-colors, 'amber-300'), map-get($custom-colors, 'primary'));
  transform: translateY(0);
  border: none;
  color: map-get($custom-colors, 'stone-800');
}

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
</style>
