<script setup lang="ts">
import BotNames from '@/models/botNames'
import BotPickStyle from '@/enums/botPickStyle'
import Bot from '@/models/bot'
import type { LobbySettings } from '@/models/interfaces/lobbySettings'
import { onMounted, ref, nextTick } from 'vue'
import { useToast } from 'vue-toastification'
import LobbyService from '@/services/LobbyService'
import NHL from '@/services/NhlService'
import CreateLobbyRequest from '@/models/createLobbyRequest'
import { useRouter } from 'vue-router'
import { addHours, format } from 'date-fns'
import '@/extensions/arrayExtensions'
import GameState from '@/enums/gameState'

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
const gameCount = ref<Number>()
const isLoading = ref(false)
const isJoiningLobby = ref(false)
const isHelpVisible = ref(false)
const isLobbySettingsVisible = ref(false)
const isCreatingLobby = ref(false)
const hasLoadedGames = ref(false)
const settings = ref<LobbySettings>({
  picksPerTeam: 1,
  bots: []
})

//hooks/methods
;(async () => {
  const latestLobby = localStorage.getItem('latestLobby')
  if (latestLobby) {
    const latestLobbyParsed: { joinCode: string; name: string } = JSON.parse(latestLobby)
    name.value = latestLobbyParsed.name
    code.value = latestLobbyParsed.joinCode
  }
  const schedule = await NHL.getSchedule(format(addHours(new Date(), -4), 'yyyy-MM-dd'))
  gameCount.value = schedule.games.filter((g) => g.gameState !== GameState.Final).length
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
    const lobby = await LobbyService.createLobby(new CreateLobbyRequest(name.value, settings.value.picksPerTeam))
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
</script>

<template>
  <div class="d-flex flex-column align-items-center justify-content-center bg-net" style="min-height: 100%; width: 100%">
    <div class="shadow overflow-hidden p-5" style="border-radius: 20px; background-color: rgba(0, 0, 0, 0.5)">
      <div style="width: 306px">
        <div class="text-center">
          <img src="/img/logo.png" style="width: 100%" />
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
              <button @click="joinLobby" class="d-block btn btn-primary w-100 fw-bold py-3 text-uppercase" :disabled="isLoading">
                <span v-if="!isJoiningLobby">Join Lobby</span>
                <b-spinner v-if="isJoiningLobby" class="m-n2" style="height: 30px; width: 30px"></b-spinner>
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
            </div>
            <div>
              <a role="button" v-if="settings.bots.length < MAX_BOTS" class="d-block text-uppercase fw-bold mt-2" @click="addBot">Add Bot</a>
            </div>
          </div>

          <div class="text-center mt-4">
            <button @click="createLobby" class="d-block btn btn-primary w-100 fw-bold py-3 text-uppercase" :disabled="isLoading">
              <span v-if="!isCreatingLobby">Create Lobby</span>
              <b-spinner v-if="isCreatingLobby" class="m-n2" style="height: 30px; width: 30px"></b-spinner>
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
            <span v-if="hasLoadedGames" class="d-block text-stone-300">({{ gameCount }} games left today)</span>
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
      <div class="mt-4 text-center" style="opacity: 0.8">
        DRAFTPUCK © {{ new Date().getFullYear() }}
        <a target="_blank" class="ms-2" href="https://ropersoftworks.com">Roper Softworks LLC</a>
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
@/services/NhlService
