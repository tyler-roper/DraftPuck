<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useToast } from '@/composables/useToast'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import VNotificationPreferenceFields from '@/components/VNotificationPreferenceFields.vue'
import { useUserStore } from '@/stores/user'
import UserService from '@/services/UserService'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { allNotificationKeys, hasPreferenceChanges, pickPreferences, type NotificationPreferences } from '@/helpers/notificationSettings'

/** The user's default notification settings: what each lobby they join starts with. */
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const toast = useToast()

const saved = computed(() => pickPreferences(currentUser.value ?? {}, allNotificationKeys))
const preferences = ref<NotificationPreferences>({ ...saved.value })
const applyToActiveLobbies = ref(false)
const isSaving = ref(false)
const isDirty = computed(() => hasPreferenceChanges(preferences.value, saved.value, allNotificationKeys) || applyToActiveLobbies.value)
const isPushAllowedHere = 'Notification' in window && Notification.permission === 'granted'

async function save() {
  isSaving.value = true
  try {
    const user = await UserService.updateNotificationPreferences(preferences.value, applyToActiveLobbies.value)
    userStore.setCurrentUser(user)
    preferences.value = { ...saved.value }
    toast.success(applyToActiveLobbies.value ? 'Defaults saved and applied to your active lobbies!' : 'Defaults saved!', { timeout: 2500 })
    applyToActiveLobbies.value = false
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't save your notification defaults."))
  } finally {
    isSaving.value = false
  }
}
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
    <VButton class="btn btn-primary fw-bold w-100 p-3 mt-3" :disabled="!isDirty" :is-loading="isSaving" @click="save">Save Notification Defaults</VButton>
  </div>
</template>
