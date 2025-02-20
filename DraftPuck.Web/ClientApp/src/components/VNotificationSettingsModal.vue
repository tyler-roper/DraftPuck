<script setup lang="ts">
import { ref } from 'vue'
import VButton from '@/components/VButton.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { useToast } from 'vue-toastification'

const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const toast = useToast()

const emit = defineEmits(['close'])
const stopPropagation = (e: Event) => e.stopPropagation()
const isNotificationSupported = 'Notification' in window

//data
const showSafari = ref(true)
const isNotificationsEnabled = ref(isNotificationSupported && Notification.permission === 'granted')
const isDirty = ref(false)
const notificationSettings = ref<UserNotificationPreferencesRequestModel>({
  drinkReceivedNotificationPreference: currentUser.value!.drinkReceivedNotificationPreference,
  drinkAwardedNotificationPreference: currentUser.value!.drinkAwardedNotificationPreference,
  chatNotificationPreference: currentUser.value!.chatNotificationPreference,
  pickingStartedNotificationPreference: currentUser.value!.pickingStartedNotificationPreference
})
const isSaving = ref(false)

//methods
async function save() {
  isSaving.value = true
  try {
    await userStore.saveUserNotificationPreferences(notificationSettings.value)
    isSaving.value = false
    close()
    toast.success('Preferences saved!')
  } catch {
    console.error('Error saving preferences.')
    isSaving.value = false
  }
}

const close = () => {
  if (!isDirty.value) return emit('close')
  if (isSaving.value) return
  if (confirm('You have unsaved changes. Exit without saving?')) emit('close')
}
async function enableAndSave() {
  isSaving.value = true
  const waitingNotice = toast("Waiting on permissions...")
  await Notification.requestPermission()
  toast.dismiss(waitingNotice)

  if (Notification.permission !== 'granted') {
    toast.error('You must allow notifications to enable them.')
    isSaving.value = false
    return
  }

  await save()
}
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click="stopPropagation">
      <div class="modal-header">
        <span class="d-block">Notification Settings</span>
        <a v-if="!isSaving" role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>
      <div class="modal-body">
        <div v-if="!isNotificationSupported" class="fs-6 fw-bold text-center">
          <div>To enable push notifications on an iOS device, add this page to your home screen.</div>
          <div class="mt-2 p-3 bg-stone-900 rounded">
            <a role="button" class="mx-3" @click="showSafari = true" :class="{ 'opacity-75': !showSafari }">Safari</a>
            <a role="button" class="mx-3" @click="showSafari = false" :class="{ 'opacity-75': showSafari }">Chrome</a>
            <img v-if="showSafari" class="w-100 mt-3" src="/img/ios-safari.png" />
            <img v-if="!showSafari" class="w-100 mt-3" src="/img/ios-chrome.png" />
          </div>
        </div>

        <div v-if="isNotificationSupported">
          <div class="notification-form-container">
            <div class="question">
              <span class="question-text">🚨 When a drink is awarded...</span>
              <input type="radio" id="rdDrinkAwarded0" v-model="notificationSettings.drinkAwardedNotificationPreference" :value="0" />
              <label for="rdDrinkAwarded0">Do not notify me.</label>

              <input type="radio" id="rdDrinkAwarded1" v-model="notificationSettings.drinkAwardedNotificationPreference" :value="1" />
              <label for="rdDrinkAwarded1">Notify me if it's mine.</label>

              <input type="radio" id="rdDrinkAwarded2" v-model="notificationSettings.drinkAwardedNotificationPreference" :value="2" />
              <label for="rdDrinkAwarded2">Always notify me.</label>
            </div>

            <div class="question">
              <span class="question-text">🍺 When a drink is assigned...</span>
              <input type="radio" id="rdDrinkAssigned0" v-model="notificationSettings.drinkReceivedNotificationPreference" :value="0" />
              <label for="rdDrinkAssigned0">Do not notify me.</label>

              <input type="radio" id="rdDrinkAssigned1" v-model="notificationSettings.drinkReceivedNotificationPreference" :value="1" />
              <label for="rdDrinkAssigned1">Notify me if it's assigned to me.</label>

              <input type="radio" id="rdDrinkAssigned2" v-model="notificationSettings.drinkReceivedNotificationPreference" :value="2" />
              <label for="rdDrinkAssigned2">Always notify me.</label>
            </div>

            <div class="question">
              <span class="question-text">💬 When there's a new message in chat...</span>
              <input type="radio" id="rdChatMessage0" v-model="notificationSettings.chatNotificationPreference" :value="0" />
              <label for="rdChatMessage0">Do not notify me.</label>

              <input type="radio" id="rdChatMessage1" v-model="notificationSettings.chatNotificationPreference" :value="1" />
              <label for="rdChatMessage1">Notify me if I'm mentioned.</label>

              <input type="radio" id="rdChatMessage2" v-model="notificationSettings.chatNotificationPreference" :value="2" />
              <label for="rdChatMessage2">Always notify me.</label>
            </div>

            <div class="question">
              <span class="question-text">📌 When picking becomes available for a new game...</span>
              <input type="radio" id="rdPickingStarted0" v-model="notificationSettings.pickingStartedNotificationPreference" :value="0" />
              <label for="rdPickingStarted0">Do not notify me.</label>

              <input type="radio" id="rdPickingStarted2" v-model="notificationSettings.pickingStartedNotificationPreference" :value="2" />
              <label for="rdPickingStarted2">Always notify me.</label>
            </div>
          </div>
        </div>
        <VButton
          v-if="isNotificationsEnabled"
          class="btn btn-primary fw-bold w-100 p-3 mt-4"
          @click="save"
          :is-loading="isSaving"
          >Save Settings</VButton
        >
        <VButton
          v-if="!isNotificationsEnabled"
          class="btn btn-primary fw-bold w-100 p-3 mt-4"
          :is-loading="isSaving"
          @click="enableAndSave"
        >
          Save &amp; Enable Notifications
        </VButton>
        <button v-if="!isNotificationSupported" class="btn btn-stone-900 text-stone-0 fw-bold w-100 p-3 mt-4" @click="close">Close</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.notification-form-container {
  margin-bottom: -0.5rem;
}

.notification-form-container .question {
  padding: 0.5rem;
  background: map-get($custom-colors, 'stone-100');
  border-radius: 0.5rem;
}

.notification-form-container .question:not(:last-child) {
  padding-bottom: 0.75rem;
  margin-bottom: 0.75rem;
}

.notification-form-container .question-text {
  font-weight: bold;
  display: block;
}

.notification-form-container input[type='radio'] {
  display: none;
}

.notification-form-container label {
  padding: 0.25rem 0.5rem;
  margin: 0.5rem 0;
  border-radius: 5px;
  background: map-get($custom-colors, 'stone-100');
  display: block;
  width: 100%;
  opacity: 0.5;
}

.notification-form-container label:last-child {
  margin-bottom: 0;
}

.notification-form-container label::before {
  content: '';
  display: inline-block;
  width: 15px;
  height: 15px;
  border-radius: 20px;
  margin-bottom: -3px;
  background-color: map-get($custom-colors, 'stone-800');
  margin-right: 0.5rem;
  border: 2px solid transparent;
}

.notification-form-container input[type='radio']:checked + label {
  font-weight: bold;
  opacity: 1;
}

.notification-form-container input[type='radio']:checked + label::before {
  background: map-get($custom-colors, 'stone-0');
  border: 4px solid map-get($custom-colors, 'primary');
}
</style>
