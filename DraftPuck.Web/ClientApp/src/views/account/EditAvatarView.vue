<script setup lang="ts">
import { computed, ref } from 'vue'
import ProfileSubsectionLayout from '@/views/layouts/ProfileSubsectionLayout.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import VUser from '@/components/VUser.vue'
import '@/extensions/arrayExtensions'
import VIcon from '@/components/VIcon.vue'
import { useToast } from 'vue-toastification'
import { useRouter } from 'vue-router'

interface SuccessMessage {
  id: number
  text: string
  isError: boolean
}

const avatarToasts = [
  "Lookin' good!",
  'New look!',
  'Fresh!',
  'Bingo!',
  "Lookin' sharp!",
  'Did you get a haircut?',
  "Lookin' fly!",
  'Glow up!',
  'Snazzy!',
  'Million bucks!',
  'Hot in here!'
]

const avatarDiscardToasts = [
  'Good choice...',
  'Yeah, not your best.',
  "Gonna be a 'No' from me.",
  'Change is scary.',
  'Smart move.',
  'In with the old...',
  "I wasn't gonna say it...",
  "I wasn't feelin' it either.",
  'Probably for the best.',
  'Good riddance.'
]

//#region data
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const userPreview = ref<User>(JSON.parse(JSON.stringify(currentUser.value)))
const isDirty = computed(() => userPreview.value.avatarPath !== currentUser.value!.avatarPath)
const isUploading = ref(false)
const messages = ref<SuccessMessage[]>([])
const isSaving = ref(false)
const toast = useToast()
const router = useRouter()
const imageUpload = ref<HTMLInputElement>()

let nextId = 1
//#endregion

//#region methods
function newImage(e: Event) {
  const input = e.target as HTMLInputElement
  if (!input.files) return

  const [file] = input.files
  if (!file) return

  const allowedTypes = ['image/png', 'image/jpeg']
  const maxSizeMB = 2
  const maxSizeBytes = maxSizeMB * 1024 * 1024

  if (!allowedTypes.includes(file.type)) {
    addMessage('Only PNG or JPEG images are allowed.', true)
    return
  }

  if (file.size > maxSizeBytes) {
    addMessage(`Image must be smaller than ${maxSizeMB}MB.`, true)
    return
  }

  isUploading.value = true

  try {
    const reader = new FileReader()
    reader.addEventListener(
      'load',
      () => {
        userPreview.value.avatarPath = (reader.result ?? '') as string
        isUploading.value = false
        addMessage(avatarToasts.random())
      },
      false
    )
    reader.readAsDataURL(file)
  } catch {
    isUploading.value = false
  }
}

function discard() {
  addMessage(avatarDiscardToasts.random())
  if (imageUpload.value) imageUpload.value.value = ''
  userPreview.value.avatarPath = currentUser.value!.avatarPath
}

function addMessage(text: string, isError: boolean = false) {
  const id = nextId++
  messages.value.push({ id, text, isError })

  setTimeout(() => {
    messages.value = messages.value.filter((msg) => msg.id !== id)
  }, 1250)
}

async function save() {
  try {
    isSaving.value = true
    await userStore.updateUser({ avatarData: userPreview.value.avatarPath })
    toast.success('Successfully updated your avatar!', { timeout: 2000 })
    router.replace(`/u/${currentUser.value!.nickname}`)
  } catch (e) {
    console.error(`Failed to update avatar: ${e}`)
  } finally {
    isSaving.value = false
  }
}
//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Avatar" :is-dirty="isDirty" @discard="discard" @save="save" :is-saving="isSaving">
    <div class="d-flex flex-column justify-content-center h-100">
      <div class="d-flex justify-content-center mt-n5 position-relative">
        <VUser display="avatar" :user="userPreview" :show-menu-on-click="false" :avatar-size-in-px="180" />
        <div v-if="isUploading" class="loading-overlay">
          <VIcon icon="hourglass" prefix="sr" class="text-stone-0 hourglass" />
        </div>
        <div class="notifications">
          <transition-group name="fade-up" tag="div" class="notifications">
            <div v-for="msg in messages" :key="msg.id" class="success-message" :class="{ 'is-error': msg.isError }">
              {{ msg.text }}
            </div>
          </transition-group>
        </div>
      </div>
      <div class="mt-4">
        <div class="text-center">
          <label for="imageUpload">
            <a role="button" class="btn border border-stone-400 bg-stone-9000" :class="{ 'o-50': isUploading || isSaving }" style="width: 155px">{{
              !isUploading ? 'Upload New Avatar' : 'Uploading...'
            }}</a>
          </label>
          <input
            id="imageUpload"
            ref="imageUpload"
            type="file"
            accept="image/png, image/jpeg"
            @change="newImage"
            :disabled="isUploading || isSaving"
          />
        </div>
      </div>
      <div class="mt-4 fs-7 text-center text-stone-400">
        <p>Only PNG or JPG images are allowed</p>
        <p>Max file size of 2MB</p>
      </div>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.placeholder {
  opacity: 0.2;
}

.text-shadow {
  text-shadow: 0 0 10px rgba(map-get($custom-colors, 'primary'), 0.5);
}

input[type='file'] {
  display: none;
}

.loading-overlay {
  position: absolute;
  z-index: 2;
  background: rgba(0, 0, 0, 0.7);
  top: 0;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.success-message {
  position: absolute;
  z-index: 2;
  top: -40px;
  left: 50%;
  font-size: 16px;
  transform: translateX(-50%) translateY(-20px);
  text-align: center;
}

.success-message.is-error {
  color: map-get($custom-colors, 'danger');
}

.fade-up-enter-active {
  animation-name: fadeUp;
  animation-duration: 1.25s;
  animation-fill-mode: forwards;
  animation-timing-function: cubic-bezier(0.23, 1, 0.32, 1);
}

.fade-up-leave-active {
  opacity: 0;
  transition: opacity 0.25s ease-out;
}

.hourglass {
  animation: spin 2s forwards cubic-bezier(0.68, -0.55, 0.27, 1.55) infinite;
  font-size: 70px;
  opacity: 0.9;
}

@keyframes fadeUp {
  0% {
    transform: translateX(-50%) translateY(0);
  }
  100% {
    transform: translateX(-50%) translateY(-20px);
  }
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }

  50% {
    transform: rotate(180deg);
  }

  100% {
    transform: rotate(360deg);
  }
}
</style>
