<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import { ref, computed, onMounted, nextTick } from 'vue'
import { email, maxLength, required } from '@vuelidate/validators'
import useVuelidate from '@vuelidate/core'
import { useToast } from '@/composables/useToast'
import authService from '@/services/AuthService'
import { getErrorMessage } from '@/helpers/requestHelpers'

const form = ref({ email: '' })
const rules = computed(() => ({ email: { required, email, maxLength: maxLength(100) } }))
const v$ = useVuelidate(rules, form)

const toast = useToast()
const isSending = ref(false)
const isSent = ref(false)
const emailInput = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  await nextTick()
  emailInput.value?.focus()
})

async function sendLink(e: Event) {
  e.preventDefault()
  v$.value.$validate()
  if (v$.value.$error) return

  try {
    isSending.value = true
    await authService.requestPasswordReset(form.value.email.trim())
    isSent.value = true
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Sorry, we couldn't send the email. Please try again."))
  } finally {
    isSending.value = false
  }
}
</script>

<template>
  <SplashLayout>
    <div v-if="isSent" class="text-center">
      <p>
        If there's an account for <strong>{{ form.email }}</strong>, we've sent it a link to reset your password.
      </p>
      <p class="text-stone-400 fs-7">It can take a minute to arrive, and it might land in spam. The link works once, for an hour.</p>
      <router-link :to="{ name: 'Login' }" class="text-white">
        <VIcon icon="caret-left" class="me-1" />
        <span>Back to log in</span>
      </router-link>
    </div>
    <form v-else @submit="sendLink">
      <p class="text-center">Enter your account's email and we'll send you a link to choose a new password.</p>
      <div class="row">
        <div class="col">
          <VInputWrapper icon="at">
            <input
              type="email"
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
          <VButton class="btn btn-light w-100" :is-loading="isSending" type="submit" loading-text="Sending...">Send reset link</VButton>
        </div>
      </div>
      <div class="row mt-4">
        <div class="col text-center">
          <router-link :to="{ name: 'Login' }" class="text-white">
            <VIcon icon="caret-left" class="me-1" />
            <span>Back to log in</span>
          </router-link>
        </div>
      </div>
    </form>
  </SplashLayout>
</template>
