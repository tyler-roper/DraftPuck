<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ProfileSubsectionLayout from './layouts/ProfileSubsectionLayout.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import VInputWrapper from '@/components/VInputWrapper.vue'
import useVuelidate from '@vuelidate/core'
import { required, email, maxLength, sameAs } from '@vuelidate/validators'
import { nickname, optionalPassword } from '@/helpers/validationHelpers'
import VIcon from '@/components/VIcon.vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'

//#region data
const userStore = useUserStore()
const toast = useToast()
const router = useRouter()
const { currentUser } = storeToRefs(userStore)
const isUpdatingAccount = ref(false)
const formElement = ref<HTMLFormElement | null>()
//#endregion

//#region methods
function discard() {
  ;((form.nickname = currentUser.value!.nickname!), (form.email = currentUser.value!.email!), (form.password = ''), (form.confirmPassword = ''))
}

async function triggerFormSubmission() {
  await updateAccount()
}

async function updateAccount() {
  try {
    v$.value.$validate()
    if (v$.value.$error) return
    isUpdatingAccount.value = true

    await userStore.updateUser(form)
    router.replace(`/u/${currentUser.value?.nickname}`)
    toast.success('Profile updated!', { timeout: 2000 })
  } catch (ex: unknown) {
    console.error(ex)
    let friendlyMessage = 'Sorry, something went wrong.'
    if (typeof ex === 'object' && ex !== null && 'response' in ex) {
      const axiosError = ex as any
      friendlyMessage = axiosError.response?.data?.title ?? friendlyMessage
    }
    toast.error(friendlyMessage)
  } finally {
    isUpdatingAccount.value = false
  }
}
//#endregion

//#region hooks
//#endregion

//#region form
const form = reactive<UpdateUserRequest>({
  nickname: currentUser.value!.nickname!,
  email: currentUser.value!.email!,
  password: '',
  confirmPassword: ''
})

const rules = computed(() => ({
  nickname: { required, nickname },
  email: { required, email, maxLength: maxLength(100) },
  password: { optionalPassword },
  confirmPassword: { sameAs: sameAs(computed(() => form.password)) }
}))

const isDirty = computed(() => {
  if (form.nickname !== currentUser.value!.nickname!) return true
  if (form.email !== currentUser.value!.email!) return true
  if (form.password !== '') return true
  if (form.confirmPassword !== '') return true
  return false
})

const v$ = useVuelidate<UpdateUserRequest>(rules, form)
//#endregion
</script>

<template>
  <ProfileSubsectionLayout
    title="Account Settings"
    @save="triggerFormSubmission"
    @discard="discard"
    :is-dirty="isDirty"
    :is-saving="isUpdatingAccount"
  >
    <div class="p-3">
      <form @submit="updateAccount" ref="formElement">
        <div class="profile-section mt-0">
          <label class="form-label title" for="nickname">Nickname</label>
          <VInputWrapper>
            <input
              type="text"
              id="nickname"
              v-model="form.nickname"
              placeholder="Wayne Gretzky"
              maxlength="25"
              class="form-control fs-5 bg-stone-900"
              :class="{ 'is-invalid': v$.nickname.$error }"
              ref="nicknameInput"
            />
          </VInputWrapper>
          <div class="d-flex align-items-center text-stone-400 mt-3">
            <VIcon icon="info" class="fs-5 me-2" />
            <span class="d-block"
              >Your nickname is yours and yours only. If you join a lobby with someone using it, they'll be automatically renamed.</span
            >
          </div>
        </div>
        <div class="hr"></div>
        <div class="profile-section">
          <label class="form-label title" for="email">Email</label>
          <VInputWrapper>
            <input
              type="text"
              id="email"
              v-model="form.email"
              placeholder="TheGreatOne@gmail.com"
              maxlength="100"
              class="form-control fs-5 bg-stone-900"
              :class="{ 'is-invalid': v$.email.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="d-flex align-items-center text-stone-400 mt-3">
          <VIcon icon="info" class="fs-5 me-2" />
          <span class="d-block"
            >Your email is used strictly for recovering a forgotten password. We will not share it, sell it, or make fun of it.</span
          >
        </div>

        <div class="hr"></div>

        <div class="profile-section">
          <span class="title">Change Password</span>
          <label class="form-label" for="password">Password <span class="text-primary small ms-2">(8+ Characters)</span></label>
          <VInputWrapper>
            <input
              type="password"
              id="password"
              v-model="form.password"
              placeholder="Password"
              maxlength="100"
              class="form-control fs-5 bg-stone-900"
              :class="{ 'is-invalid': v$.password.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="mt-3">
          <label class="form-label" for="confirmPassword">Confirm Password</label>
          <VInputWrapper>
            <input
              type="password"
              id="confirmPassword"
              v-model="form.confirmPassword"
              placeholder="Password"
              maxlength="100"
              class="form-control fs-5 bg-stone-900"
              :class="{ 'is-invalid': v$.confirmPassword.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="d-flex align-items-center text-stone-400 mt-3">
          <VIcon icon="info" class="fs-5 me-2" />
          <span class="d-block">Make it strong! Or don't. We'll never know.</span>
        </div>
        <!-- <div class="hr"></div>
        <div class="profile-section">
          <span class="title">Danger Zone</span>
          <button class="btn btn-outline-danger">Delete My Account</button>
        </div> -->
      </form>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.placeholder {
  opacity: 0.2;
}

.hr {
  margin: 30px 0;
}

.profile-section.placeholder {
  width: 100%;
  height: 100px;
  opacity: 0.15;
  margin-top: 60px;
}

.profile-section > .title {
  display: block;
  font-size: 24px;
  font-weight: bold;
  margin-bottom: 10px;
}

input {
  font-weight: 300;
}
</style>
