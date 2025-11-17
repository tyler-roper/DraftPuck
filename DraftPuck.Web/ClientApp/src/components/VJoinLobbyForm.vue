<script setup lang="ts">
import { ref, computed } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, requiredIf, maxLength, minLength, alpha } from '@vuelidate/validators'
import VInputWrapper from '@/components/VInputWrapper.vue'
import VButton from '@/components/VButton.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { nickname } from '@/helpers/validationHelpers'
import LobbyService from '@/services/LobbyService'
import { useToast } from 'vue-toastification'
import { useRouter } from 'vue-router'

//#region data
const userStore = useUserStore()
const { isLoggedIn, currentUser } = storeToRefs(userStore)
const isNotLoggedIn = computed(() => !isLoggedIn.value )
const isJoiningLobby = ref(false)
const toast = useToast()
const router = useRouter()
//#endregion

//#region form
interface JoinLobbyViewModel {
  code: string
  nickname: string
}

const form = ref<JoinLobbyViewModel>({
  code: '',
  nickname: ''
})

const rules = computed(() => ({
  code: { required, maxLength: maxLength(4), minLength: minLength(4), alpha },
  nickname: { requiredIf: requiredIf(isNotLoggedIn), maxLength: maxLength(25), nickname }
}))

const v$ = useVuelidate<JoinLobbyViewModel>(rules, form)
//#endregion

//#region methods
async function joinLobby(e: Event) {
  e.preventDefault()

  v$.value.$validate()
  if (v$.value.$error) return

  try {
    isJoiningLobby.value = true
    const lobby = await LobbyService.getLobbyByCode(form.value.code)
    const existingMember = lobby.members.find((m) => m.name.trim().toLowerCase() === form.value.nickname.trim().toLowerCase())

    if (existingMember && currentUser.value!.id !== existingMember.userId) {
      toast.error("Nickname already in use")
      isJoiningLobby.value = false
      return
    }

    await LobbyService.joinLobbyByCode(form.value.code, form.value.nickname)
    router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode }})
  } catch (e) {
    toast.error("Lobby not found.")
    isJoiningLobby.value = false
  }
}
//#endregion

//#region hooks
;(async () => {
  if (isLoggedIn.value) {
    form.value.nickname = userStore.currentUser!.nickname!
  } else {
    const latestLobbyRaw = localStorage.getItem('latestLobby')
    const latestLobby: { joinCode: string; name: string } | undefined = latestLobbyRaw ? JSON.parse(latestLobbyRaw) : undefined

    form.value.code = latestLobby?.joinCode ?? ''
    form.value.nickname = userStore.currentUser?.nickname ?? latestLobby?.name ?? ''
  }
})()
//#endregion

//#endregion
</script>

<template>
  <form @submit="joinLobby">
    <div class="row gx-3">
      <div :class="isLoggedIn ? 'col-12' : 'col-4'">
        <label class="form-label" for="lobbyCode">Code</label>
        <VInputWrapper icon="key" prefix="sr">
          <input
            type="text"
            id="lobbyCode"
            v-model="form.code"
            placeholder="Code"
            maxlength="4"
            class="form-control text-uppercase code-input dark"
            :class="{ 'is-invalid': v$.code.$error }"
            ref="lobbyCodeInput"
          />
        </VInputWrapper>
      </div>
      <div v-if="!isLoggedIn" class="col-8">
        <label class="form-label" for="nickname">Nickname</label>
        <VInputWrapper icon="user" prefix="sr">
          <input
            type="text"
            id="nickname"
            v-model="form.nickname"
            placeholder="Wayne Gretzky"
            maxlength="25"
            class="form-control dark"
            :class="{ 'is-invalid': v$.nickname.$error }"
            ref="nicknameInput"
          />
        </VInputWrapper>
      </div>
    </div>
    <div class="row mt-3">
      <div class="col">
        <VButton class="btn btn-primary w-100" :is-loading="isJoiningLobby" type="submit" loading-text="Joining Lobby...">Join Lobby</VButton>
      </div>
    </div>
  </form>
</template>

<style scoped lang="scss">
.code-input::placeholder  {
    text-transform: none !important;
}
</style>