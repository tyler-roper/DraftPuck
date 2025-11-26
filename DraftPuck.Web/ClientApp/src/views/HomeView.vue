<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VJoinLobbyForm from '@/components/VJoinLobbyForm.vue'
import VCreateLobbyForm from '@/components/VCreateLobbyForm.vue'
import VBigSwitch from '@/components/VBigSwitch.vue'
import VIcon from '@/components/VIcon.vue'
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
    <div class="d-flex flex-column h-100">
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
      <div class="mt-auto">
        <div class="hr"></div>
        <div class="btn-container my-4">
          <div class="text-center mb-3 text-stone-400">Want some rare achievements?</div>
          <a role="button" target="_blank" href="https://discord.gg/7NfZMhMt" class="btn btn-purple d-block mx-auto">
            <VIcon icon="discord" prefix="brands" class="me-2" />
            <span>Join the official Discord</span>
          </a>
          <a role="button" target="_blank" href="https://github.com/tyler-roper/DraftPuck" class="btn btn-light d-block mt-3 mx-auto">
            <VIcon icon="github" prefix="brands" class="me-2" />
            <span>Contribute on GitHub</span>
          </a>
        </div>
        <div class="hr"></div>
      </div>
      <div class="mt-auto text-center text-stone-400">
        {{ `© ${new Date().getFullYear()} DraftPuck — Tyler Roper` }}
      </div>
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

.btn-purple {
  background-color: #5865f2;
  border-color: transparent !important;
}

.btn-purple:hover {
  background-color: #4c58dd;
}

.btn-purple:active {
  background-color: #3743c5;
}

.btn-container .btn {
  width: 250px !important;
}
</style>
