<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { compareAsc, differenceInDays, isYesterday } from 'date-fns'
import VIcon from '@/components/VIcon.vue'
import LobbyService from '@/services/LobbyService'
import { useToast } from 'vue-toastification'
import ProfileSubsectionLayout from '@/views/layouts/ProfileSubsectionLayout.vue'

//#region data
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const lobbySummaries = ref<LobbySummary[]>([])
const toast = useToast()
const isLoading = ref(true)
//#endregion

//#region methods
function ago(date: Date) {
  const now = new Date()

  if (isYesterday(date)) return 'Yesterday'

  const diffInDays = differenceInDays(now, date)
  if (diffInDays < 7) return `${diffInDays}d ago`
  if (diffInDays < 30) return `${Math.floor(diffInDays / 7)}w ago`
  if (diffInDays < 365) return `${Math.floor(diffInDays / 30)}m ago`
  return `${Math.floor(diffInDays / 365)}y ago`
}

async function leave(lobby: LobbySummary) {
  const isConfirmed = confirm('Leaving this lobby will forfeit all of your picks. Are you sure?')
  if (!isConfirmed) return

  try {
    await LobbyService.leaveLobby(lobby.joinCode)
    lobbySummaries.value = lobbySummaries.value.filter((l) => l.id !== lobby.id)
    toast.success(`Left lobby ${lobby.joinCode}.`, { timeout: 2000 })
  } catch (e) {
    console.error('Failed to leave lobby:', e)
  }
}
//#endregion

//#region hooks
onMounted(async () => {
  if (!currentUser.value) return
  try {
    isLoading.value = true
    lobbySummaries.value = (await LobbyService.getLobbies()).sort((a, b) => compareAsc(b.created, a.created))
  } catch (e) {
    console.error('Error loading lobbies:', e)
  } finally {
    isLoading.value = false
  }
})
//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Lobbies" :show-save="false">
    <div v-if="isLoading" class="p-2">
      <div v-for="n in 20" :key="n" class="placeholder placeholder-wave w-100 rounded-md" style="margin-bottom: 10px; height: 39px"></div>
    </div>
    <div v-else-if="lobbySummaries.length === 0" class="h-100 d-flex align-items-center justify-content-center p-5 text-center">
      <div class="mb-5">
        <VIcon icon="empty-set" style="font-size: 150px" class="text-stone-500" />
        <span class="d-block text-uppercase fs-1 fw-bold">WOOPS!</span>
        <span class="d-block my-4 fs-3 text-stone-200">Looks like you haven't been in any lobbies yet.</span>
        <span class="d-block fs-5 text-stone-400">What have you been doing...?<br />Get out there!</span>
      </div>
    </div>
    <div v-else class="p-2">
      <div class="lobby-summary" :class="{ 'is-live-lobby': lobby.isActive }" v-for="lobby in lobbySummaries" :key="lobby.id">
        <span class="text-uppercase fw-bold ls-6">
          <router-link v-if="lobby.isActive" :to="`/lobby/${lobby.joinCode}`" class="d-flex align-items-center">
            <VIcon icon="link-alt" class="d-block me-1 fs-8" />
            <span class="d-block">{{ lobby.joinCode }}</span>
          </router-link>
          <span v-else>{{ lobby.joinCode }}</span>
        </span>
        <span :class="{ 'fw-bold ls-3': lobby.isActive }">{{ lobby.isActive ? 'LIVE' : ago(lobby.created) }}</span>
        <span><VIcon prefix="sr" icon="hockey-puck" class="me-1 text-stone-500" /> {{ lobby.gameCount }}</span>
        <span><VIcon prefix="sr" icon="users-alt" class="me-1 text-stone-500" /> {{ lobby.memberCount }}</span>
        <span>🍻 {{ lobby.drinksGiven }}</span>
        <span>🥴 {{ lobby.drinksTaken }}</span>
        <span>
          <a role="button" v-if="lobby.isActive" @click="leave(lobby)"><VIcon prefix="sr" icon="leave" class="text-stone-400" /></a>
        </span>
      </div>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.placeholder {
  opacity: 0.2;
}

.lobby-summary {
  background-color: map-get($custom-colors, 'stone-700');
  padding: 8px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
  border: 1px solid map-get($custom-colors, 'stone-400');
  box-shadow: 0 0 3px rgba(map-get($custom-colors, 'primary'), 1);
}

.lobby-summary:not(.is-live-lobby) {
  background-color: map-get($custom-colors, 'stone-800');
  opacity: 0.75;
  box-shadow: none;
}

.lobby-summary > span {
  font-size: 14px;
  display: block;
  width: 40px;
  text-align: center;
}

.lobby-summary > span:nth-child(1) {
  width: 60px;
  text-align: left;
}

.lobby-summary > span:nth-child(2) {
  width: 60px;
}

.lobby-summary > span:last-child {
  width: 17px;
  text-align: right;
}
</style>
