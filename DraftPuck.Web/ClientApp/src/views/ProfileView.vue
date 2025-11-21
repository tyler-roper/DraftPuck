<script setup lang="ts">
import { useRouter } from 'vue-router'
import { computed, onMounted, ref, watch } from 'vue'
import { useUserStore } from '@/stores/user'
import userService from '@/services/UserService'
import { storeToRefs } from 'pinia'
import VUser from '@/components/VUser.vue'
import VIcon from '@/components/VIcon.vue'
import AchievementService from '@/services/AchievementService'

const props = defineProps<{
  username?: string | null
}>()

//#region data
const router = useRouter()
const userStore = useUserStore()
const { currentUser, isLoggedIn } = storeToRefs(userStore)
const profileUser = ref<User>()
const isLoadingUser = ref(false)
const isLoadingAchievements = ref(false)
const notFound = ref(false)
const bannerPath = computed(() => `url(${profileUser.value?.banner.imagePath})`)
const allAchievements = ref<Array<Achievement>>([])
const achievementCompletionPercentage = computed(() => {
  if ((currentUser.value?.achievements?.length ?? 0) === 0) return 0
  return Math.floor(currentUser.value!.achievements.length / allAchievements.value.length)
})
//#endregion

//#region computed
const isLoading = computed(() => isLoadingUser.value || isLoadingAchievements.value)
const isSelf = computed(() => isLoggedIn.value && currentUser.value?.nickname === props.username)
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

//#region methods
async function fetchProfileData(username: string) {
  if (isSelf.value === true) {
    profileUser.value = currentUser.value
    return
  }

  try {
    isLoadingUser.value = true
    profileUser.value = await userService.getUserByName(username)
    if (!profileUser.value) notFound.value = true
  } catch (ex) {
    notFound.value = true
    console.error('Failed to load profile:', ex)
  } finally {
    isLoadingUser.value = false
  }
}

function onBreadcrumbClick() {
  try {
    router.back()
  } catch {
    router.push('/')
  }
}

async function logout() {
  await userStore.logout()
}
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
  <div class="h-100 bg-dark-gradient">
    <div v-if="notFound">User not found.</div>
    <div v-else-if="isLoading">
      <div class="header-background-placeholder bg-stone-700 placeholder placeholder-wave">
        <a role="button" @click="onBreadcrumbClick" class="d-flex align-items-center text-stone-0">
          <VIcon icon="angle-circle-left" prefix="sr" class="fs-3 text-stone-300" />
        </a>
      </div>
      <div class="px-4 text-center">
        <div class="profile-avatar placeholder placeholder-wave"></div>
        <div class="mt-3 placeholder placeholder-wave col-5 fs-1"></div>
        <div class="fs-5 mt-2 placeholder placeholder-wave col-8"></div>
      </div>
      <div class="px-4">
        <div class="profile-section placeholder placeholder-wave"></div>
        <div class="profile-section placeholder placeholder-wave"></div>
      </div>
    </div>
    <div v-else-if="profileUser">
      <div class="header-background">
        <a role="button" @click="onBreadcrumbClick" class="d-flex align-items-center text-stone-0">
          <VIcon icon="angle-circle-left" prefix="sr" class="fs-3 text-stone-300" />
        </a>
      </div>

      <div class="px-4 bg-gradient">
        <div class="profile-avatar d-flex align-items-center justify-content-center">
          <VUser display="avatar" :user="profileUser" :show-menu-on-click="false" :avatar-size-in-px="130" :show-edit-avatar="isSelf" />
        </div>
        <div class="text-center fs-1 fw-bold mt-2">{{ profileUser.nickname }}</div>
        <div class="text-center fs-7 text-stone-300 mt-n1 text-uppercase">{{ profileUser.title.text }}</div>
      </div>

      <div class="px-4">
        <div class="hr"></div>
        <div class="profile-section my-4">
          <div class="title my-1 d-flex justify-content-between align-items-center">
            <span class="d-block">Banner &amp; Title</span>
            <router-link :to="{ name: 'Banner' }" replace v-if="isSelf" role="button" class="fw-normal fs-7 fw-bold text-uppercase">
              <VIcon icon="pencil" class="me-2" />
              <span>Edit</span>
            </router-link>
          </div>
          <VUser :user="profileUser" :show-menu-on-click="false" />
        </div>
        <div class="hr"></div>

        <div class="profile-section">
          <span class="title">Achievements</span>
          <router-link
            :to="{ name: 'Achievements', params: { username: props.username }, state: { user: JSON.stringify(profileUser) } }"
            role="button"
            class="achievement-container text-stone-0"
            replace
          >
            <div class="progress-container" :class="`achievement-progress-${achievementCompletionPercentage}`">
              <div class="earned">{{ achievementCompletionPercentage }}%</div>
            </div>
            <div class="d-flex align-items-center justify-content-between flex-grow-1">
              <div class="fs-6 text-stone-0 py-3 d-flex align-items-center">
                <span class="me-1 fs-3 my-n3 me-2">🏆</span>
                <span>{{ currentUser?.achievements.length ?? 0 }}</span>
                <span class="mx-1">/</span>
                <span>{{ allAchievements.length }}</span>
                <span class="ms-2">Earned</span>
              </div>
              <div>
                <VIcon prefix="sr" icon="angle-right" />
              </div>
            </div>
          </router-link>
        </div>

        <template v-if="isSelf">
          <div class="hr"></div>
          <div class="profile-section">
            <span class="title">Account</span>
            <div class="profile-options-container">
              <router-link class="profile-option" :to="{ name: 'Settings' }" replace>
                <VIcon class="icon" prefix="rr" icon="user-gear" />
                <span>Account Settings</span>
                <VIcon class="caret" prefix="rr" icon="angle-right" />
              </router-link>
              <router-link class="profile-option" :to="{ name: 'Lobbies' }" replace>
                <VIcon class="icon" prefix="rr" icon="hockey-puck" />
                <span>Your Lobbies</span>
                <VIcon class="caret" prefix="rr" icon="angle-right" />
              </router-link>
              <router-link class="profile-option" :to="{ name: 'Discord' }" replace>
                <VIcon class="icon" prefix="brands" icon="discord" />
                <span> {{ currentUser?.discordUserId ? 'Unlink' : 'Link' }} Discord</span>
                <VIcon class="caret" prefix="rr" icon="angle-right" />
              </router-link>
              <a @click="logout" class="profile-option text-primary">
                <VIcon class="icon" prefix="rr" icon="exit" />
                <span>Logout</span>
              </a>
            </div>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@use 'sass:math';
@import '@/assets/scss/custom-colors.scss';
$color-progress: map-get($custom-colors, 'amber-500');
$color-track: map-get($custom-colors, 'stone-700');

@mixin generate-progress-classes() {
  @for $i from 0 through 100 {
    $degrees: math.div($i, 100) * 360;

    .achievement-progress-#{$i} {
      background: conic-gradient(
        from 180deg,
        $color-progress 0deg,
        $color-progress #{$degrees}deg,
        white #{$degrees}deg,
        white #{$degrees + 4}deg,
        $color-track #{$degrees + 4}deg,
        $color-track 360deg
      );
    }
  }
}

@include generate-progress-classes();

.hr {
  height: 1px;
  background: map-get($custom-colors, 'stone-800');
  margin-top: 20px;
}

.bg-dark-gradient {
  background-image: linear-gradient(to bottom, map-get($custom-colors, 'stone-1000'), map-get($custom-colors, 'stone-800'));
}

.header-background,
.header-background-placeholder {
  overflow: hidden;
  height: 150px;
  width: 100%;
  position: relative;
  opacity: 1;
}

.header-background::before,
.header-background::after {
  content: '';
  display: block;
  position: absolute;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
}

.header-background > a,
.header-background-placeholder > a {
  position: absolute;
  top: 13px;
  left: 20px;
  z-index: 10;
}

.header-background::before {
  background-image: v-bind(bannerPath);
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center;
}

.header-background::after {
  background: rgba(0, 0, 0, 0.15);
}

.profile-avatar,
.profile-avatar.placeholder {
  margin: -65px auto 0 auto;
}

.profile-avatar.placeholder {
  display: block;
  width: 130px;
  height: 130px;
  border-radius: 50%;
  position: relative;
  border: 3px solid map-get($custom-colors, 'stone-0');
}

.stat-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 0 auto 0 auto;
}

.stat {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  flex-basis: 1;
}

.stat > .stat-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 70px;
  color: map-get($custom-colors, 'stone-800');
  line-height: 100px;
}

.stat > .stat-text {
  margin-top: -75px;
  text-align: center;
  position: relative;
}

.stat > .stat-text > span:first-child {
  display: block;
  font-weight: bold;
  font-size: 35px;
}

.stat > .stat-text > span:last-child {
  display: block;
  font-size: 12px;
  color: map-get($custom-colors, 'amber-500');
  text-transform: uppercase;
  margin-top: -3px;
  width: 80px;
  line-height: 15px;
  letter-spacing: 1px;
  text-align: center;
  font-weight: bold;
}

.achievement-container {
  margin: 23px 0 32px 30px;
  padding: 5px 10px 5px 85px;
  border-radius: 10px;
  border: 1px solid map-get($custom-colors, 'stone-600');
  position: relative;
  display: flex;
  transition: 0.3s border;
}

.achievement-container:hover {
  border: 1px solid map-get($custom-colors, 'stone-200');
}

.progress-container {
  position: absolute;
  left: -30px;
  top: 50%;
  width: 100px;
  height: 100px;
  border-radius: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 10px rgba(0, 0, 0, 0.5);
}

.progress-container::before {
  content: '';
  position: absolute;
  width: calc(100% - 10px);
  height: calc(100% - 10px);
  border-radius: 50%;
  background-color: map-get($custom-colors, 'stone-900');
}

.progress-container > .earned {
  z-index: 2;
  background-color: map-get($custom-colors, 'stone-800');
  border-radius: 50%;
  width: 70px;
  height: 70px;
  padding: 3px 8px;
  font-weight: 600;
  box-shadow: 0 0 7px rgba(0, 0, 0, 0.4);
  font-size: 22px;
  letter-spacing: 1px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.profile-section {
  margin-top: 20px;
}

.profile-section.placeholder {
  width: 100%;
  height: 100px;
  opacity: 0.15;
  margin-top: 60px;
}

.profile-section > .title {
  display: block;
  font-size: 18px;
  font-weight: bold;
  margin-bottom: 10px;
}

.profile-options-container {
  border-radius: 10px;
  border: 1px solid map-get($custom-colors, 'stone-600');
  overflow: hidden;
}

.profile-option {
  display: flex;
  align-items: center;
  padding: 10px 12px;
  color: map-get($custom-colors, 'stone-0');
}

.profile-option > span {
  font-size: 16px;
  display: block;
  letter-spacing: 0.3px;
}

.profile-option > .icon {
  font-size: 24px;
  margin: 0 15px 0 10px;
}

.profile-option > .caret {
  margin-left: auto;
}

.profile-option:not(:first-child) {
  border-top: 1px solid map-get($custom-colors, 'stone-700');
}
</style>
