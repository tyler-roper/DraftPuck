<script setup lang="ts">
import BotNames from '@/models/botNames'
import BotPickStyle from '@/enums/botPickStyle'
import Bot from '@/models/bot'
import { addHours, format, isWithinInterval } from 'date-fns'
import type { LobbySettings } from '@/models/interfaces/lobbySettings'
import { onMounted, ref, nextTick, computed, watch } from 'vue'
import { useToast } from 'vue-toastification'
import LobbyService from '@/services/LobbyService'
import GameService from '@/services/GameService'
import CreateLobbyRequest from '@/models/createLobbyRequest'
import { useRouter } from 'vue-router'
import '@/extensions/arrayExtensions'
import GameState from '@/enums/gameState'
import VSwitch from '@/components/VSwitch.vue'
import { getOrdinal } from '@/helpers/gameHelpers'
import PeriodType from '@/enums/periodType'

//const
const MAX_BOTS = 10
const botPickStyleOptions = [
  { text: 'Pick Style', value: null },
  ...Object.entries(BotPickStyle)
    .filter((kvp: [string, string | number]) => isNaN(Number(kvp[1])))
    .map(([value, text]) => ({ text, value }))
]

//data
const toast = useToast()
const router = useRouter()
const codeInput = ref<HTMLInputElement | null>(null)
const nameInput = ref<HTMLInputElement | null>(null)
const code = ref('')
const name = ref('')
const gameSummaries = ref<Array<GameSummary>>([])
const isLoading = ref(false)
const isJoiningLobby = ref(false)
const isHelpVisible = ref(false)
const isLobbySettingsVisible = ref(false)
const isCreatingLobby = ref(false)
const hasLoadedGames = ref(false)
const isPlayAllGames = ref(true)
const settings = ref<LobbySettings>({
  picksPerTeam: 1,
  bots: [],
  isBotAutoPickingEnabled: false,
  gameIds: []
})

//computed
const gameCount = computed(() => gameSummaries.value.length)
const is4Nations = computed(() => {
  const today = new Date()
  const firstDay = new Date(2025, 1, 10)
  const lastDay = new Date(2025, 1, 20)
  const paddingHours = 12
  const start = addHours(firstDay, -1 * paddingHours)
  const end = addHours(lastDay, paddingHours)

  return isWithinInterval(today, { start, end })
})

//hooks/methods
;(async () => {
  const latestLobby = localStorage.getItem('latestLobby')
  if (latestLobby) {
    const latestLobbyParsed: { joinCode: string; name: string } = JSON.parse(latestLobby)
    name.value = latestLobbyParsed.name
    code.value = latestLobbyParsed.joinCode
  }
  gameSummaries.value = (await GameService.getAllGameSummaries()).filter((g) => g.gameState !== GameState.Final)
  settings.value.gameIds = gameSummaries.value.map((g) => g.id)
  hasLoadedGames.value = true
})()

onMounted(async () => {
  if (!code.value.trim().length) {
    await nextTick()
    codeInput.value?.focus()
    return
  }

  if (!name.value.trim().length) {
    await nextTick()
    nameInput.value?.focus()
    return
  } 
})

function addBot() {
  settings.value.bots.push(new Bot(getRandomBotName(), BotPickStyle.Random))
}

function removeBot(bot: Bot) {
  settings.value.bots = settings.value.bots.filter((b) => b !== bot)
}

async function joinLobby() {
  if (!isValidJoinLobby()) return

  try {
    isJoiningLobby.value = true
    const lobby = await LobbyService.getLobbyByCode(code.value)
    const existingMember = lobby.members.find((m) => m.name.trim().toLowerCase() === name.value.trim().toLowerCase())
    const userId = localStorage.getItem('userId')

    if (existingMember && existingMember.userId !== userId) {
      toast.error('Username already taken.')
      return
    }

    await LobbyService.joinLobbyByCode(code.value, name.value)
    router.push({
      name: 'Lobby',
      params: { joinCode: lobby.joinCode }
    })
  } catch {
    toast.error('Lobby not found.')
    isJoiningLobby.value = false
    return
  }
}

async function createLobby() {
  if (!isValidCreateLobby()) return

  try {
    isCreatingLobby.value = true
    const lobby = await LobbyService.createLobby(
      new CreateLobbyRequest(name.value, settings.value.picksPerTeam, settings.value.isBotAutoPickingEnabled, settings.value.gameIds)
    )
    const botPromises = settings.value.bots.map(async (b) => await LobbyService.joinLobbyByCode(lobby.joinCode, b.name, true, Number(b.botPickStyle)))
    await Promise.all(botPromises)

    router.push({
      name: 'Lobby',
      params: { joinCode: lobby.joinCode }
    })
  } catch {
    toast.error('Something went wrong.')
    isCreatingLobby.value = false
  }
}

//helpers
function isValidJoinLobby() {
  if (!name.value?.trim().length) {
    toast.error('Your name cannot be blank.')
    return false
  }

  if (code.value.length != 4) {
    toast.error('Invalid code.')
    return false
  }

  return true
}

function isValidCreateLobby() {
  if (!name.value?.trim().length) {
    toast.error('Your name cannot be blank.')
    return false
  }

  if (settings.value.picksPerTeam === null) {
    toast.error('You must set the picks per game. For infinite, choose 0.')
    return false
  }

  if (!hasLoadedGames.value) {
    toast.error('Games are still loading, one moment...')
    return false
  }

  if (gameCount.value === 0) {
   toast.error('No games remaining!')
   return false
  }

  if (settings.value.gameIds.length === 0) {
   toast.error('No games selected!')
   return false
  }

  if (settings.value.bots.some((b) => b.name.trim() === '')) {
    toast.error('Bots must have a name.')
    return false
  }

  if (settings.value.bots.some((b) => b.name.trim() === name.value.trim())) {
    toast.error('Bot names cannot match your name.')
    return false
  }

  if (settings.value.bots.filter((bot, index) => settings.value.bots.indexOf(bot) != index).length > 0) {
    toast.error('Bot names must be unique.')
    return false
  }

  if (settings.value.bots.some((b) => b.botPickStyle === null)) {
    toast.error('Bots must have a pick style.')
    return false
  }

  if (settings.value.bots.length > MAX_BOTS) {
    toast.error(`The maximum number of bots is 10.`)
    return false
  }

  return true
}

function getRandomBotName() {
  const unusedNames = BotNames.filter((botName) => !settings.value.bots.map((bot) => bot.name).includes(botName))
  return unusedNames.length ? unusedNames.random() : `Bot ${settings.value.bots.length}`
}

function tryMoveNext() {
  if (code.value.length >= 4) nameInput.value?.focus()
}


function getLogo(team: Team) {
  return `/img/logos/${team.abbreviation}.png`
}

function getTimeString(game: GameSummary) {
  let time = format(game.dateTime, 'p')

  if (game.gameState === GameState.Upcoming) return time

  const ordinal = getOrdinal(game.period, game.periodType)

  if (game.gameState === GameState.Live) {
    if (game.periodType === PeriodType.Shootout) time = 'SO'
    else {
      time = `${game.timeRemainingInPeriod} - ${ordinal}`
    }
  }
  {
    time = 'Final'
    if (game.period > 3) time += ` (${ordinal})`
  }

  return time
}

const gameIsSelected = (game: GameSummary) => settings.value.gameIds.includes(game.id)

//watch
watch(
  () => isPlayAllGames.value,
  (newValue) => {
    if (newValue === true) settings.value.gameIds = gameSummaries.value.map((g) => g.id)
  }
)
</script>

<template>
  <div class="d-flex flex-column align-items-center justify-content-center bg-net" style="min-height: 100%; width: 100%">
    <div class="shadow overflow-hidden p-5" style="border-radius: 20px; background-color: rgba(0, 0, 0, 0.5)">
      <div style="width: 306px">
        <div class="text-center">
          <img v-if="!is4Nations" src="/img/logo.png" style="width: 100%" />
          <img v-if="is4Nations" src="/img/logo-4nations.png" style="width: 100%" />
          <span class="d-block text-uppercase text-white fs-6 mt-1" style="letter-spacing: 1px">A live hockey drinking game</span>
        </div>
        <template v-if="!isLobbySettingsVisible">
          <div class="row mt-3 gx-3">
            <div class="col-4">
              <label class="form-label" for="codeInput">Lobby Code</label>
              <input id="codeInput" ref="codeInput" v-model="code" placeholder="Code" class="code-input" maxlength="4" @input="tryMoveNext" />
            </div>
            <div class="col-8">
              <label class="form-label" for="nameInput">Name</label>
              <input id="nameInput" ref="nameInput" v-model="name" placeholder="Wayne Gretzky" maxlength="20" />
            </div>

            <div class="mt-3 col-12">
              <button @click="joinLobby" class="d-block btn btn-primary w-100 fw-bold py-3 text-uppercase" :disabled="isJoiningLobby">
                <span v-if="!isJoiningLobby">Join Lobby</span>
                <span v-if="isJoiningLobby">
                  <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
                  <span class="visually-hidden" role="status">Loading...</span>
                </span>
              </button>
            </div>
          </div>

          <div class="d-flex align-items-center my-4">
            <div class="bg-stone-600 flex-grow-1" style="height: 1px"></div>
            <span class="d-block px-4 text-stone-100 fw-bold">OR</span>
            <div class="bg-stone-600 flex-grow-1" style="height: 1px"></div>
          </div>

          <div class="text-center">
            <button
              @click="isLobbySettingsVisible = true"
              class="d-block btn btn-outline-primary w-100 fw-bold py-3 text-uppercase"
              :disabled="isLoading"
            >
              <span>Create Lobby</span>
            </button>
          </div>
        </template>

        <template v-if="isLobbySettingsVisible">
          <div
            class="mt-3 py-1 w-100 text-center fs-4 text-uppercase"
            style="border-top: 1px solid rgb(100, 100, 100); border-bottom: 1px solid rgb(100, 100, 100)"
          >
            New Lobby
          </div>

          <div class="row mt-4 gx-3">
            <div class="col-7">
              <label class="form-label">Your Name</label>
              <input v-model="name" placeholder="Wayne Gretzky" maxlength="20" />
            </div>

            <div class="col-5">
              <label class="form-label">Picks Per Team</label>
              <input type="number" v-model="settings.picksPerTeam" />
            </div>
          </div>

          <div class="mt-4">
            <VSwitch v-model="isPlayAllGames" id="chkIsPlayAllGames" name="chkIsPlayAllGames" size="lg" switch>Play All Games</VSwitch>
            <div class="pt-2" v-if="!isPlayAllGames">
              <table class="fs-6 w-100">
                <tr v-for="game in gameSummaries" :key="game.id" :class="{ 'opacity-75': !gameIsSelected(game) }">
                  <td class="pe-2">
                    <input class="form-check-input" v-model="settings.gameIds" :id="'chkGame' + game.id" type="checkbox" :value="game.id" checked />
                  </td>
                  <td><img style="width: 25px" :src="getLogo(game.awayTeam)" /></td>
                  <td>{{ game.awayTeam.abbreviation }}</td>
                  <td class="px-1 fs-8">@</td>
                  <td>{{ game.homeTeam.abbreviation }}</td>
                  <td><img style="width: 25px" :src="getLogo(game.homeTeam)" /></td>
                  <td class="ps-4">{{ getTimeString(game) }}</td>
                </tr>
              </table>
            </div>
          </div>

          <div class="mt-4">
            <label class="form-label">Bots</label>
            <span v-if="settings.bots.length === 0" class="ms-2 text-stone-300">(None)</span>
            <div v-if="settings.bots.length > 0">
              <div v-for="(bot, idx) in settings.bots" :key="idx" class="mb-3 row gx-3 align-items-center">
                <div class="col-6">
                  <input type="text" class="bot-input" v-model="bot.name" />
                </div>
                <div class="col-5">
                  <select v-model="bot.botPickStyle" class="bot-input">
                    <option v-for="o in botPickStyleOptions" :key="o.text" :value="o.value">
                      {{ o.text }}
                    </option>
                  </select>
                </div>
                <div class="col-1">
                  <a role="button" @click="removeBot(bot)"><i class="fi fi-rr-x"></i></a>
                </div>
              </div>
              <div>
                <VSwitch
                  v-model="settings.isBotAutoPickingEnabled"
                  id="chkIsBotAutoPickingEnabled"
                  name="chkIsBotAutoPickingEnabled"
                  size="lg"
                  switch
                >
                  Make Bot Picks Automatically
                </VSwitch>
              </div>
            </div>
            <div>
              <a role="button" v-if="settings.bots.length < MAX_BOTS" class="d-block text-uppercase fw-bold mt-2" @click="addBot">Add Bot</a>
            </div>
          </div>

          <div class="text-center mt-4">
            <button @click="createLobby" class="d-block btn btn-primary w-100 fw-bold py-3 text-uppercase" :disabled="isCreatingLobby">
              <span v-if="!isCreatingLobby">Create Lobby</span>
              <span v-if="isCreatingLobby">
                <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
                <span class="visually-hidden" role="status">Loading...</span>
              </span>
            </button>
          </div>

          <div class="text-center mt-3">
            <a role="button" @click="isLobbySettingsVisible = false" :disabled="isLoading" class="fw-bold d-inline-flex text-decoration-none">
              <i class="fi fi-rr-caret-left d-block me-1" style="margin-top: -2px"></i>
              <span class="d-block mt-n1">Back</span>
            </a>
          </div>
        </template>
        <div class="mt-5 d-flex justify-content-between">
          <div>
            <a role="button" @click="isHelpVisible = !isHelpVisible">
              <i v-if="!isHelpVisible" class="fi fi-rr-caret-right me-1"></i>
              <i v-if="isHelpVisible" class="fi fi-rr-caret-down me-1"></i>
              How does it work?
            </a>
          </div>
          <div>
            <span v-if="hasLoadedGames" class="d-block text-stone-300">({{ gameCount }} game<span v-if="gameCount !== 1">s</span> left today)</span>
            <span v-if="!hasLoadedGames" class="d-block text-stone-300">Fetching games...</span>
          </div>
        </div>
        <div v-if="isHelpVisible" class="mt-3">
          <p class="m-0 p-0 lh-2">
            <strong>DRAFTPUCK</strong> is a drinking game that takes place during live NHL games. <br /><br />
            The rules are simple: users pick a player from each team. If your player scores, you make someone else drink a beer!
            <br /><br />
            Looking for a twist? Add some bots! Bots make their picks based on the "pick style" assigned to them, which can be anything from
            auto-picking the best player available, to choosing completely at random. If their player scores, a random user in the lobby will be
            picked to drink!
          </p>
        </div>
      </div>
      <div class="mt-4 text-center fw-bold">
        <a target="_blank" class="text-decoration-none" href="https://discord.gg/Vgj9RbetDB">Join us on Discord</a>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.bg-net {
  background-image: linear-gradient(rgba(0, 0, 0, 0.8), rgba(0, 0, 0, 0.8)), url(/img/net.jpg);
  background-position:
    center,
    -600px -700px;
  background-size: 3500px;
}

input,
select {
  border-radius: 6px;
  border: 0;
  padding: 16px;
  width: 100%;
}

.bot-input {
  padding: 8px 10px;
}

select.bot-input {
  padding: 9px 10px;
}

.code-input {
  text-transform: uppercase;
  letter-spacing: 2px;
  font-weight: bold;
}

.code-input::-webkit-input-placeholder {
  /* WebKit browsers */
  text-transform: none;
  font-weight: normal !important;
  letter-spacing: normal !important;
}

.code-input:-moz-placeholder {
  /* Mozilla Firefox 4 to 18 */
  text-transform: none;
  font-weight: normal !important;
  letter-spacing: normal !important;
}

.code-input::-moz-placeholder {
  /* Mozilla Firefox 19+ */
  text-transform: none;
  font-weight: normal !important;
  letter-spacing: normal !important;
}

.code-input:-ms-input-placeholder {
  /* Internet Explorer 10+ */
  text-transform: none;
  font-weight: normal !important;
  letter-spacing: normal !important;
}

.code-input::placeholder {
  /* Recent browsers */
  text-transform: none;
  font-weight: normal !important;
  letter-spacing: normal !important;
}
</style>