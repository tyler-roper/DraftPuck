<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import { ref, computed, nextTick, onMounted } from 'vue'
import { email, maxLength, required, sameAs } from '@vuelidate/validators'
import useVuelidate from '@vuelidate/core'
import { useRoute, useRouter } from 'vue-router'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import userService from '@/services/UserService'
import { useToast } from 'vue-toastification'
import { nickname, password } from '@/helpers/validationHelpers'
import { useUserStore } from '@/stores/user'
import type CreateAccountViewModel from '@/models/interfaces/createAccountViewModel'

const toast = useToast()
const currentRoute = useRoute()

//#region form
const form = ref<CreateAccountViewModel>({
  nickname: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const rules = computed(() => ({
  nickname: { required, nickname },
  email: { required, email, maxLength: maxLength(100) },
  password: { required, password },
  confirmPassword: { required, sameAs: sameAs(computed(() => form.value.password)) }
}))

const v$ = useVuelidate<CreateAccountViewModel>(rules, form)
//#endregion

const router = useRouter()
const userStore = useUserStore()
const isCreatingAccount = ref(false)
const nicknameInput = ref<HTMLInputElement | null>(null)
const lobbyCode = ref(currentRoute.query.lobby ? String(currentRoute.query.lobby) : null)

onMounted(async () => {
  await nextTick()
  nicknameInput.value?.focus()
})

async function createAccount(e: Event) {
  e.preventDefault()

  try {
    v$.value.$validate()
    if (v$.value.$error) return

    isCreatingAccount.value = true
    await userService.signUp(form.value)
    await userStore.login(form.value.email, form.value.password)

    const url = lobbyCode.value
      ? router.resolve(`/lobby/${lobbyCode.value}`)
      : router.resolve(currentRoute.query.redirect?.toString() || '/')
    return router.push(url)
  } catch (ex: unknown) {
    console.error(ex);
    let friendlyMessage = "Sorry, something went wrong.";
    if (typeof ex === 'object' && ex !== null && 'response' in ex) {
        const axiosError = ex as any;
        friendlyMessage = axiosError.response?.data?.title ?? friendlyMessage;
    }
    toast.error(friendlyMessage);
  } finally {
    isCreatingAccount.value = false
  }
}
</script>

<template>
  <SplashLayout :show-flavor-text="false" :increase-contrast="true">
    <p class="mb-4">
      <b class="text-uppercase">Before you hop the boards</b><br>
      We gotta know what name to put on your jersey...
    </p>
    <form @submit="createAccount">
      <div class="row gy-2">
        <div class="col-12">
          <label class="form-label" for="nickname">Nickname</label>
          <VInputWrapper icon="id-card-clip-alt">
            <input
              type="text"
              id="nickname"
              v-model="form.nickname"
              placeholder="Wayne Gretzky"
              maxlength="25"
              class="form-control"
              :class="{ 'is-invalid': v$.nickname.$error }"
              ref="nicknameInput"
            />
          </VInputWrapper>
        </div>
        <div class="col-12">
          <label class="form-label" for="email">Email</label>
          <VInputWrapper icon="at">
            <input
              type="text"
              id="email"
              v-model="form.email"
              placeholder="TheGreatOne@gmail.com"
              maxlength="100"
              class="form-control"
              :class="{ 'is-invalid': v$.email.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="col-12">
          <label class="form-label" for="password">Password <span class="text-primary small ms-2">(8+ Characters)</span></label>
          <VInputWrapper icon="lock">
            <input
              type="password"
              id="password"
              v-model="form.password"
              placeholder="Password"
              maxlength="100"
              class="form-control"
              :class="{ 'is-invalid': v$.password.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="col-12">
          <label class="form-label" for="confirmPassword">Confirm Password</label>
          <VInputWrapper icon="lock">
            <input
              type="password"
              id="confirmPassword"
              v-model="form.confirmPassword"
              placeholder="Password"
              maxlength="100"
              class="form-control"
              :class="{ 'is-invalid': v$.confirmPassword.$error }"
            />
          </VInputWrapper>
        </div>
        <div class="col-12 mt-3">
          <VButton class="btn btn-light w-100" :is-loading="isCreatingAccount" type="submit" loading-text="Creating account..."
            >Create Account</VButton
          >
        </div>
      </div>
    </form>
    <div class="row mt-4">
      <div class="col fs-7 text-center">
        <router-link :to="{ name: 'Login', query: currentRoute.query }" >
          <VIcon prefix="br" icon="angle-left" class="me-2 ms-n2"></VIcon>
          Back to login
        </router-link>
        <span class="d-inline-block mx-3 text-stone-400">or</span>
        <router-link v-if="!lobbyCode" :to="{ name: 'Home' }" class="text-stone-0">Continue as Guest</router-link>
        <router-link v-else :to="{ name: 'Lobby', params: { joinCode: lobbyCode } }" class="text-stone-0">Back to Lobby</router-link>
      </div>
    </div>
  </SplashLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.form-label {
  margin-bottom: 2px;
  color: map-get($custom-colors, 'stone-200');
  letter-spacing: 0.3px;
}

.error-text {
  position: absolute;
  bottom: -15px;
  font-size: 10px;
  font-weight: bold;
  text-transform: uppercase;
  color: var(--bs-form-invalid-color);
}
</style>
