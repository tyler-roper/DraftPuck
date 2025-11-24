<script setup lang="ts">
import { useSystemStore } from '@/stores/system'
import ProfileSubsectionLayout from '@/views/layouts/ProfileSubsectionLayout.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { format } from 'date-fns'
import VIcon from '@/components/VIcon.vue'

//#region data
const userStore = useUserStore()
const systemStore = useSystemStore()
const router = useRouter()
const { currentUser, isLoggedIn, isAdmin } = storeToRefs(userStore)
const { gitSha, appIsTestMode, currentSystemTime } = storeToRefs(systemStore)

//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Admin" :show-save="false">
    <div class="p-3">
      <div class="rounded-lg border border-stone-500 overflow-hidden">
        <div class="px-3 py-2 border-bottom border-stone-500 bg-stone-900 fs-6">
          <VIcon icon="gears" prefix="sr" class="me-2" />
          <span class="fw-bold">System Settings</span>
        </div>
        <div class="p-3">
          <div class="profile-section pt-0 mt-0">
            <span class="fw-bold">Version</span>
            <span class="d-block text-stone-400">{{ gitSha }}</span>
          </div>
          <div class="profile-section">
            <span class="fw-bold">Test Mode</span>
            <span class="d-block text-stone-400">{{ appIsTestMode ? 'Disabled' : 'Enabled' }}</span>
          </div>
          <div class="profile-section">
            <span class="fw-bold">System Time</span>
            <span class="d-block text-stone-400">{{ format(currentSystemTime, 'PPpp') }}</span>
          </div>
        </div>
      </div>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
</style>
