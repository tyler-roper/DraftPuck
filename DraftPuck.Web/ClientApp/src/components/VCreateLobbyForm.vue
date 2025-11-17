<script setup lang="ts">
import { ref, computed } from 'vue'
import useVuelidate from '@vuelidate/core'
import { helpers, required, requiredIf, maxLength, minLength, maxValue, minValue, integer } from '@vuelidate/validators'
import VInputWrapper from '@/components/VInputWrapper.vue'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import GameService from '@/services/GameService'
import GameState from '@/enums/gameState'
import { format, compareAsc } from 'date-fns'
import { getOrdinal } from '@/helpers/gameHelpers'
import PeriodType from '@/enums/periodType'
import BotPickStyle from '@/enums/botPickStyle'
import BotNames from '@/models/botNames'
import '@/extensions/arrayExtensions'
import { uniqueInArray } from '@/helpers/validationHelpers'
import LobbyService from '@/services/LobbyService'
import CreateLobbyRequest from '@/models/createLobbyRequest'
import { useToast } from 'vue-toastification'
import { useRouter } from 'vue-router'

//#region data
const maxBotCount = 5
const userStore = useUserStore()
const { isLoggedIn } = storeToRefs(userStore)
const isNotLoggedIn = computed(() => !isLoggedIn.value)
const hasLoadedGames = ref(false)
const isCreatingLobby = ref(false)
const gameSummaries = ref<Array<GameSummary>>([])
const isPlayAllGames = ref(true)
const tableHeight = computed(() => `${gameSummaries.value.length * 28.18}px`)
const botPickStyleOptions = [
  ...Object.entries(BotPickStyle)
    .filter((kvp: [string, string | number]) => isNaN(Number(kvp[1])))
    .map(([value, text]) => ({ text, value }))
]
const toast = useToast()
const router = useRouter()
//#endregion

//#region form
interface CreateLobbyViewModel {
  nickname: string
  picksPerTeam: number
  isBotAutoPickingEnabled: boolean
  gameIds: Array<number>
  bots: Array<Bot>
}

const form = ref<CreateLobbyViewModel>({
  nickname: '',
  picksPerTeam: 1,
  isBotAutoPickingEnabled: true,
  gameIds: [],
  bots: []
})

const rules = computed(() => ({
  nickname: { requiredIf: requiredIf(isNotLoggedIn), maxLength: maxLength(25) },
  picksPerTeam: { required, maxValue: maxValue(9), minValue: minValue(1), integer },
  gameIds: { required, minLength: minLength(1), maxLength: maxLength(gameSummaries.value.length) },
  bots: {
    maxLength: maxLength(5),
    $each: helpers.forEach({
      nickname: {
        required,
        maxLength: maxLength(25),
        unique: uniqueInArray(() => form.value.bots.map((bot) => bot.nickname))
      }
    })
  }
}))

const v$ = useVuelidate<CreateLobbyViewModel>(rules, form)
//#endregion

//#region hooks
;(async () => {
  if (isLoggedIn.value) {
    form.value.nickname = userStore.currentUser!.nickname!
  } else {
    const latestLobbyRaw = localStorage.getItem('latestLobby')
    const latestLobby: { joinCode: string; name: string } | undefined = latestLobbyRaw ? JSON.parse(latestLobbyRaw) : undefined

    form.value.nickname = userStore.currentUser?.nickname ?? latestLobby?.name ?? ''
  }

  gameSummaries.value = (await GameService.getAllGameSummaries())
    .filter((g) => g.gameState !== GameState.Final)
    .sort((a, b) => compareAsc(a.dateTime, b.dateTime))
  form.value.gameIds = gameSummaries.value.map((g) => g.id)
  hasLoadedGames.value = true
})()
//#endregion

//#region methods
async function createLobby(e: Event) {
  e.preventDefault()

  v$.value.$validate()
  if (v$.value.$error) return

  try {
    isCreatingLobby.value = true
    const lobby = await LobbyService.createLobby(
      new CreateLobbyRequest(form.value.nickname, form.value.picksPerTeam, true, form.value.bots, form.value.gameIds)
    )

    router.push({
      name: 'Lobby',
      params: { joinCode: lobby.joinCode }
    })
  } catch {
    toast.error('Something went wrong.')
    isCreatingLobby.value = false
  }
}

function getLogo(team: Team) {
  return `/img/logos/${team.abbreviation}.png`
}

function getTimeString(game: GameSummary) {
  let time = format(game.dateTime, 'p')

  if (game.gameState === GameState.Upcoming) return time

  const ordinal = getOrdinal(game.period, game.periodType)
  if (game.gameState === GameState.Live) {
    return game.periodType === PeriodType.Shootout ? 'SO' : `${game.timeRemainingInPeriod} - ${ordinal}`
  } else {
    return game.period > 3 ? `Final (${ordinal})` : 'Final'
  }
}

function onPlayAllGamesChange() {
  if (isPlayAllGames.value) form.value.gameIds = gameSummaries.value.map((g) => g.id)
}

function toggleGameSelection(game: GameSummary) {
  const gameId = game.id
  const index = form.value.gameIds.indexOf(gameId)

  if (index > -1) {
    form.value.gameIds.splice(index, 1)
  } else {
    form.value.gameIds.push(gameId)
  }
}

const gameIsSelected = (game: GameSummary) => form.value.gameIds.includes(game.id)

function getRandomBotName() {
  const unusedNames = BotNames.filter((botName) => !form.value.bots.map((bot) => bot.nickname).includes(botName))
  return unusedNames.length ? unusedNames.random() : `Bot ${form.value.bots.length}`
}

function addBot() {
  if (form.value.bots.length >= maxBotCount) return
  form.value.bots.push({
    nickname: getRandomBotName(),
    pickStyle: 5
  })
}

function removeBot(bot: Bot) {
  form.value.bots = form.value.bots.filter((b) => b !== bot)
}
//#endregion
</script>

<template>
  <form @submit="createLobby">
    <div class="row mb-3" v-if="!isLoggedIn">
      <div class="col-12">
        <div class="d-flex justify-content-between">
          <label class="d-block form-label" for="nickname">Nickname</label>
          <span class="d-block invalid-feedback" v-if="v$.nickname.$error">{{ v$.nickname.$errors[0].$message }}</span>
        </div>
        <VInputWrapper icon="user" prefix="sr">
          <input
            type="text"
            id="nickname"
            v-model="form.nickname"
            placeholder="Wayne Gretzky"
            maxlength="25"
            class="form-control dark"
            :class="{ 'is-invalid': v$.nickname.$error }"
            ref="nicknameInput"
          />
        </VInputWrapper>
      </div>
    </div>
    <div class="row">
      <div class="col-12">
        <div class="d-flex justify-content-between">
          <label class="d-block form-label" for="picksPerTeam">Picks Per Team</label>
          <span class="d-block invalid-feedback" v-if="v$.picksPerTeam.$error">{{ v$.picksPerTeam.$errors[0].$message }}</span>
        </div>
        <VInputWrapper icon="hockey-mask" prefix="sr">
          <input
            type="number"
            step="1"
            id="picksPerTeam"
            v-model="form.picksPerTeam"
            placeholder="#"
            class="form-control dark"
            :class="{ 'is-invalid': v$.picksPerTeam.$error }"
          />
        </VInputWrapper>
      </div>
    </div>
    <div class="row mt-3">
      <div class="col-12">
        <label class="form-label" for="picksPerTeam">Game Selection</label>
        <VInputWrapper icon="calendar" prefix="sr">
          <select v-model="isPlayAllGames" @change="onPlayAllGamesChange" class="dark">
            <option :value="true">Full Slate (Play all games)</option>
            <option :value="false">Custom Slate (Pick specific games)</option>
          </select>
        </VInputWrapper>
      </div>
      <Transition name="slide">
        <div class="col-12" v-if="!isPlayAllGames">
          <div class="px-3 py-1 mt-2 bg-stone-900 rounded">
            <table class="w-100">
              <tr v-for="game in gameSummaries" :key="game.id" :class="{ 'opacity-75': !gameIsSelected(game) }" @click="toggleGameSelection(game)">
                <td class="pe-2">
                  <VInputWrapper>
                    <div class="form-check">
                      <input
                        class="form-check-input"
                        style="transform: scale(1.5)"
                        v-model="form.gameIds"
                        :id="'chkGame' + game.id"
                        type="checkbox"
                        :value="game.id"
                        checked
                      />
                    </div>
                  </VInputWrapper>
                </td>
                <td><img style="width: 25px" :src="getLogo(game.awayTeam)" /></td>
                <td class="text-end">{{ game.awayTeam.abbreviation }}</td>
                <td class="px-1 text-center">@</td>
                <td class="text-start">{{ game.homeTeam.abbreviation }}</td>
                <td class="text-end"><img style="width: 25px" :src="getLogo(game.homeTeam)" /></td>
                <td class="text-end">{{ getTimeString(game) }}</td>
              </tr>
            </table>
          </div>
        </div>
      </Transition>
    </div>
    <div class="row mt-4">
      <div class="col-12">
        <div class="bot-container">
          <div class="title-bar">
            <span v-if="!form.bots.length">No Bots</span>
            <span v-else-if="form.bots.length === 1">1 Bot</span>
            <span v-else>{{ form.bots.length }} Bots</span>
            <a v-if="form.bots.length < maxBotCount" role="button" class="fw-bold text-primary" @click="addBot">+ Add Bot</a>
            <i v-else class="text-stone-400">Max Bots Added</i>
          </div>
          <div class="header-row" v-if="form.bots.length">
            <span>Name</span>
            <span>Picking Strategy</span>
          </div>
          <div class="bot-rows">
            <div v-for="(bot, idx) in form.bots" :key="idx" class="bot-row">
              <div class="bot-name">
                <VInputWrapper icon="user-robot" prefix="sr" small>
                  <input
                    v-model="bot.nickname"
                    placeholder="Name"
                    class="form-control dark borderless"
                    :class="{ 'is-invalid': v$.bots.$each?.$response?.$data[idx]?.nickname.$error }"
                  />
                </VInputWrapper>
              </div>
              <div class="bot-strategy">
                <VInputWrapper small icon="check">
                  <select v-model="bot.pickStyle" class="dark borderless">
                    <option v-for="o in botPickStyleOptions" :key="o.text" :value="o.value">
                      {{ o.text }}
                    </option>
                  </select>
                </VInputWrapper>
              </div>
              <div class="bot-delete">
                <a role="button" @click="removeBot(bot)"><VIcon icon="delete" prefix="sr" class="text-stone-400 fs-5" /></a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="row mt-3">
      <div class="col-12">
        <VButton class="btn btn-primary w-100" :is-loading="isCreatingLobby" type="submit" loading-text="Creating Lobby...">Create Lobby</VButton>
      </div>
    </div>
  </form>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.bot-container {
  border-radius: 15px;
  background-color: map-get($custom-colors, 'stone-900');
  overflow: hidden;
}

.bot-container .title-bar {
  padding: 10px 15px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.bot-container .title-bar > * {
  display: block;
}

.bot-container .title-bar > button {
  padding-top: 3px;
  padding-bottom: 3px;
}

.bot-container .header-row {
  background-color: rgba(map-get($custom-colors, 'stone-800'), 0.7);
  padding: 5px 15px;
  text-transform: uppercase;
  font-size: 10px;
  color: map-get($custom-colors, 'stone-400');
  display: flex;
}

.bot-container .bot-row {
  background-color: rgba(map-get($custom-colors, 'stone-700'), 0.7);
  padding: 5px 15px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid map-get($custom-colors, 'stone-600');
  border-bottom: 1px solid map-get($custom-colors, 'stone-800');
}

.bot-container .bot-row:first-child {
  border-top: none;
}

.bot-container .bot-row:last-child {
  border-bottom: none;
}

.bot-container .bot-row .bot-name,
.bot-container .header-row > span:first-child {
  width: 50%;
}

.bot-container .bot-row .bot-strategy,
.bot-container .header-row > span:last-child {
  width: 35%;
}

.bot-container .header-row > span:last-child {
  margin-left: 15px;
}

.code-input::placeholder {
  text-transform: none !important;
}

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
