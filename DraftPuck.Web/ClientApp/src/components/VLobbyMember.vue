<script setup lang="ts">
//#region imports
import { useLobbyStore } from '@/stores/lobby'
import { addSeconds } from 'date-fns'
import { storeToRefs } from 'pinia'
import { ref, computed } from 'vue'
import { useToast } from 'vue-toastification'
import BotPickStyle from '@/enums/botPickStyle'
import LobbyService from '@/services/LobbyService'
import { useRouter } from 'vue-router'
//#endregion

//#region props
const props = defineProps<{
  member: LobbyMember
}>()

const member = computed(() => props.member)
//#endregion

//#region refs
const lobbyStore = useLobbyStore()
const toast = useToast()
const { lobby, currentUserId, isLobbyAdmin } = storeToRefs(lobbyStore)
const { assignDrink, changeName: changeNameInStore, removeLobbyMember } = lobbyStore
const lastNameChange = ref(new Date(-1))
const router = useRouter()
//#endregion

//computed
const currentMember = computed(() => lobby.value!.members.find((m) => m.userId === currentUserId.value)!)
const pendingDrinksByMember = computed(() => member.value.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)))
const drinksGivenByMember = computed(() => member.value.picks.flatMap((p) => p.drinks.filter((d) => !!d.recipientLobbyMemberId)))
const drinksTakenByMember = computed(() =>
  !lobby.value ? [] : lobby.value.members.flatMap((m) => m.picks.flatMap((p) => p.drinks)).filter((d) => d.recipientLobbyMemberId === member.value.id)
)
const isCurrentMember = computed(() => currentUserId.value === member.value.userId)
const pendingDrinksForCurrentMember = computed(() => currentMember.value!.picks.flatMap((p) => p.drinks.filter((d) => !d.recipientLobbyMemberId)))
const isCreator = computed(() => lobby.value!.createdBy === member.value.userId)
const isBot = computed(() => member.value.isBot)
const iconClass = computed(() => ({
  'text-stone-0': isCurrentMember.value,
  'text-stone-700': isBot.value,
  'text-danger': !isCurrentMember.value && !isBot.value,
  'fi-sr-user': !isBot.value && !isCreator.value,
  'fi-sr-user-crown': isCreator.value,
  'fi-sr-user-robot': isBot.value
}))

const isLoggedIn = computed(() => member.value.isGuest === false)
const bannerStyle = computed(() => {
  return member.value.isGuest !== false
    ? {}
    : { 'background-image': `url(${member.value.banner?.imagePath})` }
})

const avatarStyle = computed(() => {
  return member.value.isGuest !== false && member.value.avatarPath
    ? {}
    : { 'background-image': `url(${member.value.avatarPath})` }
})

const memberTitle = computed(() => {
  if (member.value.isBot === true) return `Strategy: ${BotPickStyle[member.value.botPickStyle]}`
  if (member.value.isGuest === true) {
    if (isCurrentMember.value === true) return "You (Guest)"
    else return "Guest"
  }
  return member.value.title?.text
})

//hooks/methods
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

async function giveDrink(recipient: LobbyMember) {
  const pendingDrinks = pendingDrinksForCurrentMember.value
  if (pendingDrinks.length === 0) return
  const drink = pendingDrinks[0]
  await assignDrink(drink.id, recipient.id)
}

async function leaveLobby() {
  const isConfirmed = confirm("Leaving this lobby will forfeit all of your picks. Are you sure?");
  if (!isConfirmed) return

  try {
    await LobbyService.leaveLobby(lobby.value!.joinCode)
    toast.success(`Left lobby ${lobby.value!.joinCode}.`, { timeout: 2000 });
    router.push({name: 'Home' })
  } catch (e) {
    console.error("Failed to leave lobby:", e);
  }
}
</script>

<template>
  <div class="dropdown">
    <a
      role="button"
      :style="bannerStyle"
      :class="{ 'border-blue': isCurrentMember, 'border-danger': !isCurrentMember && !isBot }"
      class="bg-stone-100 member-link text-stone-900 text-decoration-none d-flex fs-6 p-2"
      data-bs-toggle="dropdown"
    >
      <div class="icon" :class="{ 'bg-blue': isCurrentMember }" :style="avatarStyle">
        <i v-if="!member.avatarPath || member.isGuest" class="fi me-2 d-block" :class="iconClass"></i>
      </div>

      <div class="d-block">
        <span class="name d-block fw-bold text-start" :class="{ 'is-logged-in': isLoggedIn }">{{ member.name }}</span>
        <span class="title d-block text-stone-500 mt-n1 fs-7" :class="{ 'is-logged-in': isLoggedIn }">
          {{ memberTitle }}
        </span>
      </div>

      <span class="d-flex ms-auto align-items-center" style="cursor: default">
        <span
          :class="{ 'bg-primary': pendingDrinksByMember.length > 0 }"
          class="bg-white d-block text-center border border-stone-200 mx-1 rounded"
          style="width: 50px"
        >
          <span class="d-block fs-9 text-uppercase fw-bold">To Give</span>
          <span class="fw-bold">🍺 {{ pendingDrinksByMember.length }}</span>
        </span>

        <span class="bg-white d-block text-center border border-stone-200 mx-1 rounded" style="width: 50px">
          <span class="d-block fs-9 text-uppercase fw-bold">Given</span>
          <span class="fw-bold">🍻 {{ drinksGivenByMember.length }}</span>
        </span>

        <span class="bg-white d-block text-center border border-stone-200 mx-1 rounded" style="width: 50px">
          <span class="d-block fs-9 text-uppercase fw-bold">Taken</span>
          <span class="fw-bold">🥴 {{ drinksTakenByMember.length }}</span>
        </span>
      </span>

      <ul class="dropdown-menu">
        <template v-if="isCurrentMember">
          <li>
            <a role="button" class="fw-bold dropdown-item" @click="changeName"><i class="fi fi-rr-id-badge me-2"></i>Change Name</a>
            <a role="button" class="fw-bold dropdown-item text-danger" @click="leaveLobby"><i class="fi fi-rr-exit me-2"></i>Leave Lobby</a>
          </li>
        </template>
        <template v-if="!isCurrentMember">
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
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.member-link {
  width: 100%;
  display: flex;
  align-items: center;
  border-left: 10px solid map-get($custom-colors, 'stone-700');
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center;
}

.member-link.show {
  background-color: map-get($custom-colors, 'stone-200');
}

.member-link .icon {
  display: flex;
  width: 50px;
  height: 50px;
  background: map-get($custom-colors, 'stone-0');
  border-radius: 100%;
  align-items: center;
  justify-content: center;
  font-size: 40px;
  overflow: hidden;
  padding-left: 8px;
  padding-top: 25px;
  margin-right: 10px;
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  border: 2px solid map-get($custom-colors, 'stone-0');
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

.name.is-logged-in {
  text-shadow:
    0 0 5px black,
    0 1px black;
  color: map-get($custom-colors, 'stone-0');
  font-size: 18px !important;
}



.title.is-logged-in {
  text-shadow:
    0 0 5px black,
    0 1px black;
  padding: 0px 2px;
  letter-spacing: 0.5px;
  background: rgba(0, 0, 0, 0.3);
  font-size: 10px !important;
  text-transform: uppercase;
  font-weight: bold;
  text-align: left;
  color: map-get($custom-colors, 'stone-200') !important;
}
</style>
