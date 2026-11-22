<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import VButton from '@/components/VButton.vue'
import VNotificationPreferenceFields from '@/components/VNotificationPreferenceFields.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { useToast } from '@/composables/useToast'
import { useFirebaseStore } from '@/stores/firebase'
import LobbyService from '@/services/LobbyService'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { hasPreferenceChanges, lobbyNotificationKeys, pickPreferences, type LobbyNotificationPreferences } from '@/helpers/notificationSettings'

/** This lobby's notification settings. They start as the user's defaults (set in Account Settings) when they join. */
const props = defineProps<{ joinCode: string }>()
const emit = defineEmits(['close'])

const userStore = useUserStore()
const { isLoggedIn } = storeToRefs(userStore)
const toast = useToast()
const firebase = useFirebaseStore()

const stopPropagation = (e: Event) => e.stopPropagation()
const isNotificationSupported = 'Notification' in window

const showSafari = ref(true)
const isNotificationsEnabled = ref(isNotificationSupported && Notification.permission === 'granted')
const isLoading = ref(true)
const isSaving = ref(false)
const saved = ref<LobbyNotificationPreferences>(pickPreferences({}, lobbyNotificationKeys))
const preferences = ref<LobbyNotificationPreferences>({ ...saved.value })
const isDirty = computed(() => hasPreferenceChanges(preferences.value, saved.value, lobbyNotificationKeys))

onMounted(async () => {
  try {
    saved.value = pickPreferences(await LobbyService.getNotificationPreferences(props.joinCode), lobbyNotificationKeys)
    preferences.value = { ...saved.value }
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't load your notification settings."))
    emit('close')
  } finally {
    isLoading.value = false
  }
})

async function save() {
  isSaving.value = true
  try {
    saved.value = pickPreferences(await LobbyService.updateNotificationPreferences(props.joinCode, preferences.value), lobbyNotificationKeys)
    preferences.value = { ...saved.value }
    await firebase.initialize()
    emit('close')
    toast.success('Preferences saved!', { timeout: 2000 })
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't save your notification settings."))
  } finally {
    isSaving.value = false
  }
}

const close = () => {
  if (isSaving.value) return
  if (!isDirty.value || confirm('You have unsaved changes. Exit without saving?')) emit('close')
}

async function enableAndSave() {
  isSaving.value = true
  const waitingNotice = toast.show('Waiting on permissions...', { timeout: false })
  await Notification.requestPermission()
  toast.dismiss(waitingNotice)

  if (Notification.permission !== 'granted') {
    toast.error('You must allow notifications to enable them.')
    isSaving.value = false
    return
  }

  isNotificationsEnabled.value = true
  await save()
}
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click="stopPropagation">
      <div class="modal-header justify-content-between">
        <span class="d-block">Notification Settings</span>
        <a v-if="!isSaving" role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>
      <div class="modal-body dark">
        <div v-if="!isNotificationSupported" class="fs-6 fw-bold text-center">
          <div>To enable push notifications on an iOS device, add this page to your home screen.</div>
          <div class="mt-2 p-3 bg-stone-900 rounded">
            <a role="button" class="mx-3" @click="showSafari = true" :class="{ 'opacity-75': !showSafari }">Safari</a>
            <a role="button" class="mx-3" @click="showSafari = false" :class="{ 'opacity-75': showSafari }">Chrome</a>
            <img v-if="showSafari" class="w-100 mt-3" src="/img/ios-safari.png" />
            <img v-if="!showSafari" class="w-100 mt-3" src="/img/ios-chrome.png" />
          </div>
        </div>

        <div v-if="isNotificationSupported && !isLoading">
          <p class="fs-7 text-stone-400 mb-3">
            These settings are for this lobby only.
            <template v-if="isLoggedIn">
              New lobbies start with your
              <router-link :to="{ name: 'Settings' }" @click="emit('close')">default settings</router-link>.
            </template>
          </p>
          <VNotificationPreferenceFields v-model="preferences" :keys="lobbyNotificationKeys" id-prefix="lobby" dark />
        </div>
        <template v-if="isNotificationSupported">
          <VButton v-if="isNotificationsEnabled" class="btn btn-primary fw-bold w-100 p-3 mt-4" @click="save" :is-loading="isSaving || isLoading"
            >Save Settings</VButton
          >
          <VButton v-else class="btn btn-primary fw-bold w-100 p-3 mt-4" :is-loading="isSaving || isLoading" @click="enableAndSave">
            Save &amp; Enable Notifications
          </VButton>
        </template>
        <button v-if="!isNotificationSupported" class="btn btn-stone-900 text-stone-0 fw-bold w-100 p-3 mt-4" @click="close">Close</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Dark, like the rest of the lobby (the shared modal body is white).
.modal-body.dark {
  color: map-get($custom-colors, 'stone-0');
  background: map-get($custom-colors, 'stone-850');
}
</style>
