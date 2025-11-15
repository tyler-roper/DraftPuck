<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import { computed, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import VIcon from '@/components/VIcon.vue'

const props = withDefaults(
  defineProps<{
    user?: User
    showMenuOnClick?: boolean
    display?: 'banner' | 'avatar'
    class?: string
    avatarSizeInPx?: number
    showEditAvatar?: boolean
  }>(),
  {
    showMenuOnClick: false,
    display: 'banner',
    avatarSizeInPx: 75,
    showEditAvatar: false
  }
)

interface UserLink {
  action?: Function
  path?: string
  text: string
  class?: string
  iconPrefix?: string
  icon?: string
}

//data
const userStore = useUserStore()
const router = useRouter()
const route = useRoute()
const toast = useToast()
const { currentUser } = storeToRefs(userStore)
const user = computed(() => props.user ?? currentUser.value)
const bannerPath = computed(() => (user.value?.banner?.imagePath ? `url(${user.value.banner.imagePath})` : ''))
const avatarPath = computed(() => (user.value?.avatarPath ? `url(${user.value.avatarPath})` : ''))
const isLoggingOut = ref(false)
const isSelf = computed(() => currentUser.value && user.value?.id === currentUser.value?.id)
const isClickable = computed(() => props.showMenuOnClick === true && isSelf.value === true)
const loginPath = computed(() => (route.params.joinCode ? `/login?lobby=${route.params.joinCode}` : '/login'))
const joinPath = computed(() => (route.params.joinCode ? `/join?lobby=${route.params.joinCode}` : '/join'))
const fontSize = computed(() => {
  const BASE_SIZE = 50
  const BASE_DIVISOR = 1.35
  const CORRECTION_FACTOR = 0.01

  const currentSize = props.avatarSizeInPx
  const dynamicDivisor = BASE_DIVISOR + (currentSize - BASE_SIZE) * CORRECTION_FACTOR
  const constrainedDivisor = Math.max(1.2, Math.min(dynamicDivisor, 2.0))
  const finalFontSize = currentSize / constrainedDivisor

  return `${finalFontSize}px`
})

const loggedInUserLinks = ref<Array<UserLink>>([
  { path: `/u/${user.value?.nickname}`, text: 'Profile', iconPrefix: 'sr', icon: 'user-pen' },
  { path: '/account/lobbies', text: 'My Lobbies', iconPrefix: 'sr', icon: 'users-alt' },
  { action: logout, text: 'Logout', class: 'text-primary', iconPrefix: 'sr', icon: 'exit' }
])

const guestUserLinks = ref<Array<UserLink>>([
  { path: loginPath.value, text: 'Log In', iconPrefix: 'sr', icon: 'sign-in-alt' },
  { path: joinPath.value, text: 'Sign Up', iconPrefix: 'sr', icon: 'hockey-puck', class: 'text-primary' }
])

const userLinks = computed(() => {
  if (isSelf.value !== true) return []
  if (currentUser.value?.isGuest !== false) return guestUserLinks.value
  return loggedInUserLinks.value
})

async function logout() {
  try {
    isLoggingOut.value = true
    const name = userStore.currentUser!.nickname!.toString()
    await userStore.logout()
    router.push({ name: 'Home' })
    toast.success(`See ya next time${name ? `, ${name}` : ''}!`, { timeout: 2000 })
  } catch {
    isLoggingOut.value = false
  }
}

async function userLinkClickHandler(userLink: UserLink) {
  if (userLink.action) await userLink.action()
  if (userLink.path) router.push(userLink.path)
}
</script>

<template>
  <div v-if="isLoggingOut" class="logging-out">
    <div class="mx-auto d-flex flex-column align-items-center">
      <div class="spinner-border text-white" style="width: 100px; height: 100px" role="status">
        <span class="visually-hidden">Logging out...</span>
      </div>
      <span class="text-center d-block mx-auto mt-3 fs-5 fw-bold">Logging out...</span>
    </div>
  </div>

  <div class="dropdown" :class="props.class">
    <button
      v-if="display === 'banner'"
      class="btn rounded-0 banner d-flex p-2 fs-6 text-stone-900 align-items-center w-100"
      :style="{ 'background-image': bannerPath }"
      :class="{ 'pointer-events-none': !isClickable }"
      id="dropdownMenuButton"
      type="button"
      aria-haspopup="true"
      aria-expanded="false"
      :data-bs-toggle="isClickable ? 'dropdown' : ''"
    >
      <div class="avatar me-2" :class="{ default: !user?.avatarPath }" :style="{ 'background-image': avatarPath }">
        <i class="fi fi-sr-user"></i>
      </div>

      <div class="d-block text-start">
        <span class="d-block text-start fw-bold text-stone-0 fs-5 nickname">{{ user?.nickname }}</span>
        <span class="d-inline-block text-start text-stone-200 title text-uppercase fw-bold">{{ user?.title.text }}</span>
      </div>
    </button>

    <div v-else-if="display === 'avatar'" class="position-relative">
      <button
        id="dropdownMenuButton"
        class="btn avatar position-relative"
        :style="{ width: `${avatarSizeInPx}px`, height: `${avatarSizeInPx}px`, 'background-image': avatarPath }"
        :class="{ default: !user?.avatarPath, 'pointer-events-none': !isClickable }"
        aria-haspopup="true"
        aria-expanded="false"
        :data-bs-toggle="showMenuOnClick && isSelf ? 'dropdown' : ''"
      ></button>
      <router-link v-if="showEditAvatar && isSelf" class="edit-badge" :to="{ name: 'Avatar' }" replace>
        <VIcon icon="pencil" prefix="sr" class="text-stone-0 fs-6" />
      </router-link>
    </div>

    <div class="dropdown-menu w-100" aria-labelledby="dropdownMenuButton">
      <a
        v-for="(userLink, idx) in userLinks"
        :key="idx"
        class="dropdown-item"
        :class="userLink.class"
        role="button"
        @click="userLinkClickHandler(userLink)"
      >
        <VIcon v-if="userLink.icon" class="me-2" :prefix="userLink.iconPrefix ?? 'rr'" :icon="userLink.icon" />{{ userLink.text }}</a
      >
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.pointer-events-none {
  pointer-events: none !important;
  cursor: none;
}

.logging-out {
  width: 100dvw;
  height: 100dvh;
  position: fixed;
  top: 0;
  left: 0;
  z-index: 10;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar {
  display: flex;
  width: 50px;
  height: 50px;
  background: map-get($custom-colors, 'stone-0');
  border-radius: 50%;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background-repeat: no-repeat;
  background-position: center center;
  background-size: cover;
  border: 2px solid white;
  box-shadow: 0 0 3px rgba(0, 0, 0, 1);
  color: map-get($custom-colors, 'stone-700');
}

.avatar.default {
  background-image: none;
  font-size: 40px;
}

.avatar:not(.default) > i {
  display: none;
}

.avatar.default > i {
  display: block;
  font-size: v-bind(fontSize);
  transform: translateY(20%);
}

.menu-container {
  position: absolute;
  top: 100%;
  left: 0;
  width: 100%;
  background-color: map-get($custom-colors, 'stone-800');
  z-index: 10;
  margin: 0 !important;
  border-radius: 0 !important;
  overflow-y: scroll !important;
  box-shadow: 0 0 50px black;
}

.banner {
  overflow: hidden;
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center;
  box-shadow: 0 0 7px rgba(0, 0, 0, 0.3);
}

.nickname {
  text-shadow:
    0 0 5px black,
    0 1px black;
  height: 20px;
}

.title {
  text-shadow:
    0 0 5px black,
    0 1px black;
  padding: 0px 2px;
  letter-spacing: 0.5px;
  background: rgba(0, 0, 0, 0.3);
  font-size: 10px;
}

.user-menu-button {
  text-align: center;
  padding: 15px;
  border-top: 1px solid map-get($custom-colors, 'stone-700');
  display: block;
  width: 100%;
  font-size: 20px;
  font-weight: bold;
}

.user-menu-button:first-child {
  border-top: none;
}

.dropdown-menu {
  padding: 0 !important;
  box-shadow: 0 0 15px rgba(0, 0, 0, 0.6);
}

.dropdown-item {
  font-size: 14px;
  padding: 10px 15px !important;
  font-weight: bold;
  border-top: 1px solid map-get($custom-colors, 'stone-800');
}

.dropdown-item:first-child {
  border-top: none;
}

.dropdown-item:hover {
  background-color: map-get($custom-colors, 'stone-800');
}

.edit-badge {
  position: absolute;
  bottom: 0px;
  right: 5px;
  background-color: map-get($custom-colors, 'primary');
  box-shadow: 0 0 10px rgba(0,0,0, 0.6);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 35px;
  height: 35px;
  border: 0;
}
</style>
