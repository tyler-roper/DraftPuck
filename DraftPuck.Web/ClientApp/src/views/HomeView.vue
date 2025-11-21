<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VJoinLobbyForm from '@/components/VJoinLobbyForm.vue'
import VCreateLobbyForm from '@/components/VCreateLobbyForm.vue'
import VBigSwitch from '@/components/VBigSwitch.vue'
import { ref } from 'vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import VUser from '@/components/VUser.vue'

//#region data
const userStore = useUserStore()
const { isLoggedIn } = storeToRefs(userStore)

type View = 'join' | 'create' | 'x'
const views = ref<Array<{ value: View; label: string }>>([
  { value: 'join', label: 'Join Lobby' },
  { value: 'create', label: 'Create Lobby' }
])
const view = ref<View>('join')
//#endregion
</script>

<template>
  <SplashLayout :show-header-text="false" :show-flavor-text="false" :align-to-bottom="false" :increase-contrast="true" :padding="3">
    <div class="form-container p-3">
      <VBigSwitch class="mt-n5" :options="views" v-model="view" />
      <VUser v-if="isLoggedIn" :show-menu-on-click="true" class="my-3 rounded-md" />
      <VJoinLobbyForm v-if="view === 'join'" class="mt-3" />
      <VCreateLobbyForm v-if="view === 'create'" class="mt-3" />
    </div>
    <div v-if="!isLoggedIn" class="text-center mt-3">
      <router-link :to="{ name: 'Login' }" class="fw-bold">Log in</router-link>
      <span class="d-inline-block mx-2">or</span>
      <router-link :to="{ name: 'Join' }" class="fw-bold">join now for free</router-link>
      <span>!</span>
    </div>
  </SplashLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.form-container {
  background: map-get($custom-colors, 'stone-800');
  border-radius: 15px;
  border: 6px solid map-get($custom-colors, 'stone-900');
}
</style>
