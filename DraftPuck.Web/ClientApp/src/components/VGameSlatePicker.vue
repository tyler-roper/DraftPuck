<script setup lang="ts">
import { computed, ref } from 'vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import { formatGameClock } from '@/helpers/gameHelpers'
import { teamLogoUrl } from '@/helpers/teamLogos'

const props = defineProps<{
  games: GameSummary[]
}>()

/** The games the lobby plays: every game (a full slate), or the ones ticked in a custom slate. */
const gameIds = defineModel<number[]>({ required: true })

const isPlayAllGames = ref(true)
// The custom slate's table slides open to its full height.
const tableHeight = computed(() => `${props.games.length * 28.18}px`)

function onPlayAllGamesChange() {
  if (isPlayAllGames.value) gameIds.value = props.games.map((g) => g.id)
}

function toggleGame(game: GameSummary) {
  gameIds.value = isSelected(game) ? gameIds.value.filter((id) => id !== game.id) : [...gameIds.value, game.id]
}

const isSelected = (game: GameSummary) => gameIds.value.includes(game.id)
</script>

<template>
  <div class="row mt-3">
    <div class="col-12">
      <label class="form-label" for="gameSelection">Game Selection</label>
      <VInputWrapper icon="calendar" prefix="sr">
        <select id="gameSelection" v-model="isPlayAllGames" @change="onPlayAllGamesChange" class="dark">
          <option :value="true">Full Slate (Play all games)</option>
          <option :value="false">Custom Slate (Pick specific games)</option>
        </select>
      </VInputWrapper>
    </div>
    <Transition name="slide">
      <div class="col-12" v-if="!isPlayAllGames">
        <div class="px-3 py-1 mt-2 bg-stone-900 rounded">
          <table class="w-100">
            <tr v-for="game in games" :key="game.id" :class="{ 'opacity-75': !isSelected(game) }" @click="toggleGame(game)">
              <td class="pe-2">
                <VInputWrapper>
                  <div class="form-check">
                    <input
                      class="form-check-input"
                      style="transform: scale(1.5)"
                      v-model="gameIds"
                      :id="'chkGame' + game.id"
                      type="checkbox"
                      :value="game.id"
                    />
                  </div>
                </VInputWrapper>
              </td>
              <td><img style="width: 25px" :src="teamLogoUrl(game.awayTeam)" /></td>
              <td class="text-end">{{ game.awayTeam.abbreviation }}</td>
              <td class="px-1 text-center">@</td>
              <td class="text-start">{{ game.homeTeam.abbreviation }}</td>
              <td class="text-end"><img style="width: 25px" :src="teamLogoUrl(game.homeTeam)" /></td>
              <td class="text-end">{{ formatGameClock(game) }}</td>
            </tr>
          </table>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.slide-enter-active,
.slide-leave-active {
  transition: all 0.2s ease-in-out;
  overflow: hidden;
}

.slide-enter-from,
.slide-leave-to {
  max-height: 0;
}

.slide-enter-to,
.slide-leave-from {
  max-height: v-bind(tableHeight);
}

@media (hover: hover) {
  tr:hover > td {
    cursor: pointer;
    background-color: map-get($custom-colors, 'stone-800');
  }
}
</style>
