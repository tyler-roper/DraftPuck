<script setup lang="ts">
import VIcon from '@/components/VIcon.vue'
import VNotificationPreferenceFields from '@/components/VNotificationPreferenceFields.vue'
import { allNotificationKeys, type NotificationPreferences } from '@/helpers/notificationSettings'

/** The user's default notification settings (what each lobby they join starts with). The page that shows them saves them. */
const preferences = defineModel<NotificationPreferences>('preferences', { required: true })
const applyToActiveLobbies = defineModel<boolean>('applyToActiveLobbies', { required: true })

const isPushAllowedHere = 'Notification' in window && Notification.permission === 'granted'
</script>

<template>
  <div>
    <div class="d-flex align-items-center text-stone-400 mb-3">
      <VIcon icon="info" class="fs-5 me-2" />
      <span class="d-block">
        Every lobby you join starts with these. You can change them for one lobby from its 🔔 button.
        <template v-if="!isPushAllowedHere"> Notifications aren't turned on for this device yet; turn them on from a lobby.</template>
      </span>
    </div>
    <VNotificationPreferenceFields v-model="preferences" :keys="allNotificationKeys" id-prefix="defaults" />
    <div class="form-check mt-4">
      <input id="applyToActiveLobbies" v-model="applyToActiveLobbies" class="form-check-input" type="checkbox" />
      <label class="form-check-label" for="applyToActiveLobbies">Also apply to the lobbies I'm in now</label>
    </div>
  </div>
</template>
