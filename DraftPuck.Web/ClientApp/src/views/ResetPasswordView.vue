<script setup lang="ts">
import SplashLayout from '@/views/layouts/SplashLayout.vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import { ref, computed, onMounted, nextTick } from 'vue'
import { required, sameAs } from '@vuelidate/validators'
import useVuelidate from '@vuelidate/core'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from '@/composables/useToast'
import authService from '@/services/AuthService'
import { password } from '@/helpers/validationHelpers'
import { getErrorMessage } from '@/helpers/requestHelpers'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const token = computed(() => (typeof route.query.token === 'string' ? route.query.token : ''))

const form = ref({ password: '', confirmPassword: '' })
const rules = computed(() => ({
  password: { required, password },
  confirmPassword: { required, sameAs: sameAs(computed(() => form.value.password)) }
}))
const v$ = useVuelidate(rules, form)

const isSaving = ref(false)
const passwordInput = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  await nextTick()
  passwordInput.value?.focus()
})

async function savePassword(e: Event) {
  e.preventDefault()
  v$.value.$validate()
  if (v$.value.$error) return

  try {
    isSaving.value = true
    await authService.resetPassword(token.value, form.value.password)
    toast.success('Your password has been changed. Log in with your new password.')
    router.replace({ name: 'Login' })
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Sorry, we couldn't change your password. Please try again."))
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <SplashLayout>
    <div v-if="!token" class="text-center">
      <p>This reset link is incomplete. Try opening the link from the email again, or request a new one.</p>
      <router-link :to="{ name: 'ForgotPassword' }">Request a new link</router-link>
    </div>
    <form v-else @submit="savePassword">
      <p class="text-center">Choose a new password for your account.</p>
      <div class="row">
        <div class="col">
          <label class="form-label" for="password">New Password <span class="text-primary small ms-2">(8+ Characters)</span></label>
          <VInputWrapper icon="lock">
            <input
              type="password"
              id="password"
              v-model="form.password"
              placeholder="New password"
              maxlength="25"
              autocomplete="new-password"
              class="form-control"
              :class="{ 'is-invalid': v$.password.$error }"
              ref="passwordInput"
            />
          </VInputWrapper>
        </div>
      </div>
      <div class="row mt-3">
        <div class="col">
          <label class="form-label" for="confirmPassword">Confirm New Password</label>
          <VInputWrapper icon="lock">
            <input
              type="password"
              id="confirmPassword"
              v-model="form.confirmPassword"
              placeholder="New password"
              maxlength="25"
              autocomplete="new-password"
              class="form-control"
              :class="{ 'is-invalid': v$.confirmPassword.$error }"
            />
          </VInputWrapper>
        </div>
      </div>
      <div class="row mt-3">
        <div class="col">
          <VButton class="btn btn-light w-100" :is-loading="isSaving" type="submit" loading-text="Saving...">Change password</VButton>
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
