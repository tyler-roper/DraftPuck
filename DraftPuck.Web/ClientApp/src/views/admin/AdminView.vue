<script setup lang="ts">
import { useSystemStore } from '@/stores/system'
import ProfileSubsectionLayout from '@/views/layouts/ProfileSubsectionLayout.vue'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import VIcon from '@/components/VIcon.vue'

//#region data
const systemStore = useSystemStore()
const { gitSha, appIsTestMode, currentSystemTime } = storeToRefs(systemStore)

//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Admin" :show-save="false">
    <div class="p-3">
      <div class="profile-section">
        <span class="title">Account</span>
        <div class="profile-options-container">
          <router-link class="profile-option" :to="{ name: 'AdminUsers' }" replace>
            <VIcon class="icon" prefix="rr" icon="users" />
            <span>Users</span>
            <VIcon class="caret" prefix="rr" icon="angle-right" />
          </router-link>
          <router-link class="profile-option" :to="{ name: 'AdminLobbies' }" replace>
            <VIcon class="icon" prefix="rr" icon="hockey-puck" />
            <span>Lobbies</span>
            <VIcon class="caret" prefix="rr" icon="angle-right" />
          </router-link>
        </div>
      </div>
      <div class="hr my-3"></div>
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
            <span class="d-block text-stone-400">{{ appIsTestMode ? 'Enabled' : 'Disabled' }}</span>
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
