<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import { ref, computed, onMounted, nextTick } from 'vue'
import { maxLength, required } from '@vuelidate/validators'
import useVuelidate from '@vuelidate/core'
import { useUserStore } from '@/stores/user'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'

//#region form
interface LoginViewModel {
  email: string
  password: string
}

const form = ref<LoginViewModel>({
  email: '',
  password: ''
})

const rules = computed(() => ({
  email: { required, maxLength: maxLength(100) },
  password: { required, maxLength: maxLength(100) }
}))

const v$ = useVuelidate<LoginViewModel>(rules, form)
//#endregion

const userStore = useUserStore()
const router = useRouter()
const toast = useToast()
const currentRoute = useRoute()
const lobbyCode = ref(currentRoute.query.lobby ? String(currentRoute.query.lobby) : null)

const isLoggingIn = ref(false)
const emailInput = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  await userStore.initialize()
  if (userStore.isLoggedIn) router.replace('/')
  await nextTick()
  emailInput.value?.focus()
})

async function login(e: Event) {
  e.preventDefault()

  try {
    v$.value.$validate()
    if (v$.value.$error) return

    isLoggingIn.value = true
    await userStore.login(form.value.email, form.value.password)

    if (userStore.isLoggedIn) {
      const url = lobbyCode.value ? router.resolve(`/lobby/${lobbyCode.value}`) : router.resolve(currentRoute.query.redirect?.toString() || '/')
      router.replace(url)
      toast.success(`Welcome back, ${userStore.currentUser!.nickname}!`)
    } else throw new Error('Invalid login.')
  } catch {
    toast.error("Login failed. Please make sure you've entered your email and password correctly.")
    isLoggingIn.value = false
  }
}
</script>

<template>
  <SplashLayout>
    <form @submit="login">
      <div class="row">
        <div class="col">
          <VInputWrapper icon="at">
            <input
              type="text"
              v-model="form.email"
              placeholder="Email"
              maxlength="100"
              class="form-control"
              :class="{ 'is-invalid': v$.email.$error }"
              ref="emailInput"
            />
          </VInputWrapper>
        </div>
      </div>
      <div class="row mt-3">
        <div class="col">
          <VInputWrapper icon="lock">
            <input
              type="password"
              v-model="form.password"
              placeholder="Password"
              maxlength="100"
              class="form-control"
              :class="{ 'is-invalid': v$.password.$error }"
            />
          </VInputWrapper>
        </div>
      </div>
      <div class="row mt-3">
        <div class="col">
          <VButton class="btn btn-light w-100" :is-loading="isLoggingIn" type="submit" loading-text="Logging in...">Log In</VButton>
        </div>
      </div>
      <div class="row mt-3">
        <div class="col fs-7 text-center">
          <span class="text-stone-400">Tired of sitting on the bench?</span>
          <router-link :to="{ name: 'Join', query: currentRoute.query }" class="ms-2">Join now for free.</router-link>
        </div>
      </div>
      <div class="row mt-4">
        <div class="col text-center">
          <router-link v-if="!lobbyCode" :to="{ name: 'Home' }" class="text-white">
            <span>Continue as Guest</span>
            <VIcon icon="caret-right" class="ms-1" />
          </router-link>
          <router-link v-else :to="{ name: 'Lobby', params: { joinCode: lobbyCode } }" class="text-white">
            <VIcon icon="caret-left" class="me-1" />
            <span>Return to lobby</span>
          </router-link>
        </div>
      </div>
    </form>
  </SplashLayout>
</template>
