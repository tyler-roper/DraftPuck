<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import ProfileSubsectionLayout from './layouts/ProfileSubsectionLayout.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { differenceInDays, isToday, isYesterday } from 'date-fns'
import VIcon from '@/components/VIcon.vue'
import AchievementService from '@/services/AchievementService'
import UserService from '@/services/UserService'
import { useRouter } from 'vue-router'

const props = defineProps<{
  username?: string | null
}>()

//#region data
const userStore = useUserStore()
const router = useRouter()
const { currentUser, isLoggedIn } = storeToRefs(userStore)
const isLoadingUser = ref(false)
const isLoadingAchievements = ref(false)
const allAchievements = ref<Array<Achievement>>([])
const user = ref<User>()
const achievementList = computed<Array<UserAchievement>>(() => {
  return allAchievements.value
    .map((a) => ({
      achievementId: a.id,
      uniqueIdentifier: a.uniqueIdentifier,
      friendlyName: a.friendlyName,
      description: a.description,
      dateEarned: currentUser.value?.achievements.find((ua) => ua.achievementId === a.id)?.dateEarned
    }))
    .sort((a, b) => Number(!!b.dateEarned) - Number(!!a.dateEarned))
})

const isLoading = computed(() => isLoadingUser.value || isLoadingAchievements.value)
const isSelf = computed(() => isLoggedIn.value && currentUser.value?.nickname === props.username)
//#endregion

//#region methods
function ago(date: Date) {
  const now = new Date()

  if (isToday(date)) return 'Today'
  if (isYesterday(date)) return 'Yesterday'

  const diffInDays = differenceInDays(now, date)
  if (diffInDays < 7) return `${diffInDays}d ago`
  if (diffInDays < 30) return `${Math.floor(diffInDays / 7)}w ago`
  if (diffInDays < 365) return `${Math.floor(diffInDays / 30)}m ago`
  return `${Math.floor(diffInDays / 365)}y ago`
}

async function fetchProfileData(username: string) {
  if (isSelf.value === true) {
    user.value = currentUser.value
    return
  }

  const routerStateUserJson = router.options.history.state.user as string | undefined
  if (routerStateUserJson) {
    const routerStateUser = JSON.parse(routerStateUserJson)
    if (routerStateUser.nickname === username) {
      user.value = routerStateUser
      return
    }
  }

  try {
    isLoadingUser.value = true
    user.value = await UserService.getUserByName(username)
    if (!user.value) router.replace(`/u/${username}`)
  } catch (ex) {
    console.error('Failed to load achievements for profile:', ex)
  } finally {
    isLoadingUser.value = false
  }
}
//#endregion

//#region hooks
onMounted(async () => {
  try {
    isLoadingAchievements.value = true
    allAchievements.value = await AchievementService.getAllAchievements()
  } catch (e) {
    console.error('Error loading achievements:', e)
  } finally {
    isLoadingAchievements.value = false
  }
})
//#endregion

//#region watchers
watch(
  () => props.username,
  (newUsername) => {
    if (newUsername) {
      fetchProfileData(newUsername)
    }
  },
  { immediate: true }
)
//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Achievements" :show-save="false">
    <div v-if="isLoading" class="p-2">
      <div v-for="n in 20" :key="n" class="placeholder placeholder-wave w-100 rounded-md" style="margin-bottom: 10px; height: 100px"></div>
    </div>
    <div v-else class="p-3">
      <div
        v-for="achievement in achievementList"
        :key="achievement.achievementId"
        class="rounded-md bg-gradient mb-3 d-flex overflow-hidden border border-stone-500 shadow"
        :class="{ 'o-50': !achievement.dateEarned }"
      >
        <div class="fs-1 py-2 bg-stone-900 d-flex align-items-center justify-content-center" style="width: 60px; flex: 0 0 60px">
          <span v-if="achievement.dateEarned" class="text-shadow">🏆</span>
          <VIcon v-if="!achievement.dateEarned" class="text-stone-500" icon="lock" prefix="sr" />
        </div>
        <div class="py-2 px-3 flex-grow-1 d-flex flex-column justify-content-center">
          <span class="d-block fw-bold text-uppercase fs-5 ls-3" :class="{ ' text-stone-400': !achievement.dateEarned }">{{
            achievement.friendlyName
          }}</span>
          <span class="d-block text-stone-400 mt-n1 fs-8" v-if="achievement.dateEarned">Earned {{ ago(achievement.dateEarned) }}</span>
          <span v-if="achievement.dateEarned" class="d-block text-stone-200 fs-6 mt-2">{{ achievement.description }}</span>
        </div>
      </div>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.placeholder {
  opacity: 0.2;
}

.text-shadow {
  text-shadow: 0 0 10px rgba(map-get($custom-colors, 'primary'), 0.5);
}
</style>
