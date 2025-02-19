<script setup lang="ts">
import BotPickStyle from '@/enums/botPickStyle'
import Bot from '@/models/bot'
import BotNames from '@/models/botNames'
import { useLobbyStore } from '@/stores/lobby'
import { addSeconds, format } from 'date-fns'
import { storeToRefs } from 'pinia'
import { ref, computed, nextTick } from 'vue'
import { POSITION, useToast } from 'vue-toastification'

//const
const botPickStyleOptions = [
  { text: 'Pick Style', value: undefined },
  ...Object.entries(BotPickStyle)
    .filter((kvp: [string, string | number]) => isNaN(Number(kvp[1])))
    .map(([value, text]) => ({ text, value }))
]

//data
const lobbyStore = useLobbyStore()
const toast = useToast()

const { lobby, currentUserId, isLobbyAdmin } = storeToRefs(lobbyStore)
const { assignDrink, changeName: changeNameInStore, removeLobbyMember, addBot } = lobbyStore
const botNameInput = ref<HTMLInputElement | null>(null)
const isAddingBot = ref(false)
const botName = ref('')
const botPickStyle = ref<number>()
const lastNameChange = ref(new Date(-1))

//computed
const currentMember = computed<LobbyMember | undefined>(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))
const pendingDrinksForCurrentMember = computed(() => (currentMember.value ? getPendingDrinksByMember(currentMember.value) : []))
const bots = computed(() => lobby.value?.members.filter((m) => m.isBot) ?? [])
const createdTimeAsString = computed(() => format(lobby.value!.created, 'PP'))

//hooks/methods
async function copyInvite() {
  const code = lobby.value!.joinCode
  try {
    await navigator.clipboard.writeText(`Join my DRAFTPUCK lobby! Code: ${code}\n\nhttps://draftpuck.com/lobby/${code}`)
    toast.success('Copied invite to clipboard!', { position: POSITION.TOP_CENTER })
  } catch {
    toast.error('Cannot copy')
  }
}

async function changeName() {
  if (Number(addSeconds(lastNameChange.value, 15)) > Number(new Date())) {
    toast.error('One name change per 15 seconds.')
    return
  }

  const newName = prompt('Name', currentMember.value!.name)
  if (!newName) return

  if (lobby.value!.members.some((m) => m.name.toLowerCase() === newName.trim().toLowerCase())) {
    toast.error('Name already taken.')
    return
  }

  lastNameChange.value = new Date()
  await changeNameInStore(newName.trim())
}

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

async function giveDrink(recipient: LobbyMember) {
  const pendingDrinks = pendingDrinksForCurrentMember.value
  if (pendingDrinks.length === 0) return
  const drink = pendingDrinks[0]
  await assignDrink(drink.id, recipient.id)
}

//helpers
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

function getPendingDrinksByMember(member: LobbyMember) {
  return member.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId))
}

function getDrinksGivenByMember(member: LobbyMember) {
  return member.picks.flatMap((p) => p.drinks.filter((d) => !!d.recipientLobbyMemberId))
}

function getDrinksTakenByMember(member: LobbyMember) {
  if (!lobby.value) return []
  return lobby.value.members.flatMap((m) => m.picks.flatMap((p) => p.drinks)).filter((d) => d.recipientLobbyMemberId === member.id)
}

function isCurrentMember(member: LobbyMember) {
  return currentUserId.value === member.userId
}
</script>

<template>
  <div style="overflow-y: scroll" class="bg-stone-300 text-stone-800 d-flex flex-column">
    <div
      class="bg-stone-150 p-3 ls-2 d-flex align-items-center"
      style="z-index: 2; position: sticky; top: 0; border-bottom: 1px solid rgba(0, 0, 0, 0.1)"
    >
      <div class="fs-3 me-2">🏒</div>
      <div>
        <span class="d-block mb-n2">Code</span>
        <div class="text-decoration-none text-stone-900 d-flex align-items-center">
          <span class="fw-bold fs-4 text-uppercase">{{ lobby!.joinCode }}</span>
          <button class="d-block btn btn-primary px-1 py-0 ms-2 text-uppercase fw-bold" @click="copyInvite">Invite</button>
        </div>
      </div>
      <div class="ms-auto fw-bold">
        <span> {{ createdTimeAsString }}</span>
      </div>
    </div>

    <div class="bg-stone-100">
      <div class="dropdown" v-for="member in lobby!.members" :key="member.id">
        <a role="button" class="member-link text-stone-700 text-decoration-none d-flex fs-6" data-bs-toggle="dropdown">
          <i
            v-if="lobby!.createdBy !== member.userId && !member.isBot"
            class="fi fi-sr-user me-2 d-block text-blue"
            style="margin-top: 2px; height: 1px"
          ></i>
          <i v-if="member.isBot" class="fi fi-sr-user-robot me-2 d-block text-stone-700" style="margin-top: 2px; height: 1px"></i>
          <i v-if="lobby!.createdBy === member.userId" class="fi fi-sr-crown me-2 d-block text-amber-500" style="margin-top: 2px; height: 1px"></i>

          <span class="d-block" :class="{ 'fw-bold': currentUserId === member.userId }">{{ member.name }}</span>

          <span class="d-flex ms-auto" style="cursor: default">
            <span class="d-block text-end" style="width: 50px">
              <span class="me-1">🚨</span>
              <span class="fw-bold">{{ getPendingDrinksByMember(member).length }}</span>
            </span>

            <span class="d-block text-end" style="width: 50px" data-bs-toggle="tooltip" data-bs-placement="top" data-bs-title="Drinks Given">
              <span class="me-1">👍</span>
              <span class="fw-bold">{{ getDrinksGivenByMember(member).length }}</span>
            </span>

            <span class="d-block text-end" style="width: 50px" data-bs-toggle="tooltip" data-bs-placement="top" data-bs-title="Drinks Taken">
              <span v-if="!member.isBot">
                <span class="me-1">👎</span>
                <span class="fw-bold">{{ getDrinksTakenByMember(member).length }}</span>
              </span>
            </span>
          </span>

          <ul class="dropdown-menu">
            <template v-if="isCurrentMember(member)">
              <li>
                <a role="button" class="fw-bold dropdown-item" @click="changeName"><i class="fi fi-rr-id-badge me-2"></i>Change Name</a>
              </li>
            </template>
            <template v-if="!isCurrentMember(member)">
              <li v-if="pendingDrinksForCurrentMember.length > 0 && !member.isBot">
                <a role="button" class="fw-bold dropdown-item text-blue" @click="giveDrink(member)"><span class="me-2">🍺</span>Give a drink!</a>
              </li>
              <li v-if="isLobbyAdmin">
                <a role="button" class="fw-bold dropdown-item text-danger" @click="removeLobbyMember(member.id)"
                  ><i class="fi fi-sr-remove-user me-2"></i>Remove User</a
                >
              </li>
            </template>
          </ul>
        </a>
      </div>
      <div class="p-2" v-if="isLobbyAdmin && !isAddingBot && bots.length < 10">
        <button class="btn btn-primary text-uppercase fw-bold text-stone-900 py-0 px-1" @click="showAddBot">+ Add Bot</button>
      </div>
      <div class="p-2 d-flex justify-content-between" v-if="isLobbyAdmin && isAddingBot">
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
          <a role="button" class="fw-bold text-danger" @click="cancelAddBot">Cancel</a>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.member-link {
  display: flex !important;
  padding: 10px !important;
  font-size: 0.9rem;
  background-color: white;
}

.member-link.show {
  background-color: map-get($custom-colors, 'stone-200');
}

.dropdown-menu.show {
  padding: 0 !important;
  box-shadow: 0 0 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  border: 2px solid map-get($custom-colors, 'stone-200');
  overflow: hidden;
}

.dropdown-item {
  padding: 10px !important;
  background-color: map-get($custom-colors, 'stone-0');
  color: map-get($custom-colors, 'stone-900');
}

.dropdown-item:hover,
.dropdown-item:focus {
  background-color: map-get($custom-colors, 'stone-100');
}

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
