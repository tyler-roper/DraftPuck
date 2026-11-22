<script setup lang="ts">
import { ref, computed, onMounted, toRef } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import useVuelidate from '@vuelidate/core'
import { helpers, required, requiredIf, maxLength, minLength, maxValue, minValue, integer } from '@vuelidate/validators'
import VInputWrapper from '@/components/VInputWrapper.vue'
import VButton from '@/components/VButton.vue'
import VGameSlatePicker from '@/components/VGameSlatePicker.vue'
import VLobbyBotsEditor from '@/components/VLobbyBotsEditor.vue'
import { useLobbyFormNickname } from '@/composables/useLobbyFormNickname'
import { useToast } from '@/composables/useToast'
import { useUserStore } from '@/stores/user'
import { useGameSummariesStore } from '@/stores/gameSummaries'
import { MAX_BOTS } from '@/helpers/botHelpers'
import { uniqueInArray } from '@/helpers/validationHelpers'
import LobbyService from '@/services/LobbyService'
import CreateLobbyRequest from '@/models/createLobbyRequest'

interface CreateLobbyViewModel {
  nickname: string
  picksPerTeam: number
  gameIds: number[]
  bots: Bot[]
}

const toast = useToast()
const router = useRouter()
const gameSummariesStore = useGameSummariesStore()
const { gameSummaries } = storeToRefs(gameSummariesStore)
const { seemsLoggedIn } = storeToRefs(useUserStore())
const isCreatingLobby = ref(false)

const form = ref<CreateLobbyViewModel>({
  nickname: '',
  picksPerTeam: 1,
  gameIds: [],
  bots: []
})

// The server validates all of this too; these are for quick feedback.
const rules = computed(() => ({
  nickname: { requiredIf: requiredIf(() => !seemsLoggedIn.value), maxLength: maxLength(25) },
  picksPerTeam: { required, maxValue: maxValue(9), minValue: minValue(1), integer },
  gameIds: { required, minLength: minLength(1), maxLength: maxLength(gameSummaries.value.length) },
  bots: {
    maxLength: maxLength(MAX_BOTS),
    $each: helpers.forEach({
      nickname: {
        required,
        maxLength: maxLength(25),
        unique: uniqueInArray(() => form.value.bots.map((bot) => bot.nickname))
      }
    })
  }
}))

const v$ = useVuelidate<CreateLobbyViewModel>(rules, form)
const invalidBotNicknames = computed(() => form.value.bots.map((_, i) => !!v$.value.bots.$each?.$response?.$data[i]?.nickname.$error))
const { whenSignedIn } = useLobbyFormNickname(toRef(form.value, 'nickname'))

onMounted(async () => {
  await gameSummariesStore.loadGameSummaries()
  form.value.gameIds = gameSummaries.value.map((s) => s.id)
})

async function createLobby(e: Event) {
  e.preventDefault()

  isCreatingLobby.value = true
  await whenSignedIn()
  isCreatingLobby.value = false

  v$.value.$validate()
  if (v$.value.$error) return

  try {
    isCreatingLobby.value = true
    const lobby = await LobbyService.createLobby(
      new CreateLobbyRequest(form.value.nickname, form.value.picksPerTeam, true, form.value.bots, form.value.gameIds)
    )

    router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } })
  } catch {
    toast.error('Something went wrong.')
    isCreatingLobby.value = false
  }
}
</script>

<template>
  <form v-if="gameSummaries.length" @submit="createLobby">
    <div class="row mb-3" v-if="!seemsLoggedIn">
      <div class="col-12">
        <div class="d-flex justify-content-between">
          <label class="d-block form-label" for="nickname">Nickname</label>
          <span class="d-block invalid-feedback" v-if="v$.nickname.$error">{{ v$.nickname.$errors[0].$message }}</span>
        </div>
        <VInputWrapper icon="user" prefix="sr">
          <input
            type="text"
            id="nickname"
            v-model="form.nickname"
            placeholder="Wayne Gretzky"
            maxlength="25"
            class="form-control dark"
            :class="{ 'is-invalid': v$.nickname.$error }"
          />
        </VInputWrapper>
      </div>
    </div>
    <div class="row">
      <div class="col-12">
        <div class="d-flex justify-content-between">
          <label class="d-block form-label" for="picksPerTeam">Picks Per Team</label>
          <span class="d-block invalid-feedback" v-if="v$.picksPerTeam.$error">{{ v$.picksPerTeam.$errors[0].$message }}</span>
        </div>
        <VInputWrapper icon="hockey-mask" prefix="sr">
          <input
            type="number"
            step="1"
            id="picksPerTeam"
            v-model="form.picksPerTeam"
            placeholder="#"
            class="form-control dark"
            :class="{ 'is-invalid': v$.picksPerTeam.$error }"
          />
        </VInputWrapper>
      </div>
    </div>
    <VGameSlatePicker v-model="form.gameIds" :games="gameSummaries" />
    <div class="row mt-4">
      <div class="col-12">
        <VLobbyBotsEditor v-model="form.bots" :invalid-nicknames="invalidBotNicknames" />
      </div>
    </div>
    <div class="row mt-3">
      <div class="col-12">
        <VButton class="btn btn-primary w-100" :is-loading="isCreatingLobby" type="submit" loading-text="Creating Lobby...">Create Lobby</VButton>
      </div>
    </div>
  </form>
  <div v-else class="text-center fs-5 fw-bold text-stone-300">No games tonight,<br />check back tomorrow!</div>
</template>
