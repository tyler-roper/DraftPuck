<script setup lang="ts">
import BotPickStyle from '@/enums/botPickStyle'
import Bot from '@/models/bot'
import BotNames from '@/models/botNames'
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { ref, computed, nextTick } from 'vue'
import VLobbyMember from '@/components/VLobbyMember.vue'

//const
const botPickStyleOptions = [
  { text: 'Pick Style', value: undefined },
  ...Object.entries(BotPickStyle)
    .filter((kvp: [string, string | number]) => isNaN(Number(kvp[1])))
    .map(([value, text]) => ({ text, value }))
]

//data
const lobbyStore = useLobbyStore()

const { lobby, isLobbyAdmin, currentUserId } = storeToRefs(lobbyStore)
const { addBot } = lobbyStore
const botNameInput = ref<HTMLInputElement | null>(null)
const isAddingBot = ref(false)
const botName = ref('')
const botPickStyle = ref<number>()

//computed
const currentMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const bots = computed(() => lobby.value?.members.filter((m) => m.isBot) ?? [])
const membersSorted = computed(() =>
  [...lobby.value!.members].sort((a, b) => {
    return Number(isCurrentMember(b)) - Number(isCurrentMember(a)) || Number(isCreator(b)) - Number(isCreator(a)) || Number(a.isBot) - Number(b.isBot)
  })
)

//hooks/methods
async function tryAddBot() {
  if (!botName.value.length || !botPickStyle.value) return
  const bot = new Bot(botName.value, Number(botPickStyle.value))
  cancelAddBot()
  await addBot(bot)
}

async function showAddBot() {
  isAddingBot.value = true
  botName.value = getRandomBotName()
  await nextTick()
  botNameInput.value?.focus()
}

function cancelAddBot() {
  isAddingBot.value = false
  botPickStyle.value = undefined
  botName.value = ''
}

//helpers
function isCurrentMember(member: LobbyMember) {
  return member.id === currentMember.value!.id
}

function isCreator(member: LobbyMember) {
  return lobby.value!.createdBy === member.userId
}

function getRandomBotName() {
  const unusedNames = BotNames.filter(
    (botName) =>
      !lobby
        .value!.members.filter((m) => m.isBot)
        .map((bot) => bot.name)
        .includes(botName)
  )
  return unusedNames.length ? unusedNames.random() : `Bot ${lobby.value!.members.filter((m) => m.isBot).length}`
}
</script>

<template>
  <div style="overflow-y: scroll" class="text-stone-800 d-flex flex-column">
    <div
      class="d-none d-sm-flex bg-stone-150 p-3 ls-2 align-items-center"
      style="z-index: 2; position: sticky; top: 0; border-bottom: 1px solid rgba(0, 0, 0, 0.1)"
    >
      <div class="fs-3 me-2">🏒</div>
      <div>
        <span class="d-block mb-n2">Code</span>
        <div class="text-decoration-none text-stone-900 d-flex align-items-center">
          <span class="fw-bold fs-4 text-uppercase">{{ lobby!.joinCode }}</span>
        </div>
      </div>
    </div>
    <div class="bg-stone-100">
      <VLobbyMember v-for="member in membersSorted" :key="member.id" :member="member"></VLobbyMember>
      <div class="p-2 bg-stone-900" v-if="isLobbyAdmin && !isAddingBot && bots.length < 10">
        <button class="btn btn-primary text-uppercase fw-bold text-stone-900 py-0 px-1" @click="showAddBot">+ Add Bot</button>
      </div>
      <div class="p-2 bg-stone-900 d-flex justify-content-between" v-if="isLobbyAdmin && isAddingBot">
        <div style="width: 40%">
          <input ref="botNameInput" v-model="botName" class="w-100" />
        </div>
        <div style="width: 26%">
          <select v-model="botPickStyle" class="w-100">
            <option v-for="option in botPickStyleOptions" :value="option.value" :key="option.text">{{ option.text }}</option>
          </select>
        </div>
        <div class="d-flex align-items-center justify-content-between" style="width: 33%">
          <button class="btn btn-primary fw-bold" @click="tryAddBot">Add</button>
          <a role="button" class="fw-bold text-stone-0" @click="cancelAddBot">Cancel</a>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
input,
select {
  padding: 6px;
  border: 1px solid #ccc;
  border-radius: 6px;
}

select {
  padding-top: 7px;
  padding-bottom: 7px;
}
</style>
