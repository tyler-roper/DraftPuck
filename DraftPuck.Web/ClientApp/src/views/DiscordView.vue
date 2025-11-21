<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ProfileSubsectionLayout from './layouts/ProfileSubsectionLayout.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import '@/extensions/arrayExtensions'
import VIcon from '@/components/VIcon.vue'
import { useToast } from 'vue-toastification'
import { useRouter, useRoute } from 'vue-router'
import DiscordService from '@/services/DiscordService'

//#region data
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const toast = useToast()
const router = useRouter()
const route = useRoute()
const state = ref(route.query.state?.toString())
const isCallback = ref(!!state.value)
const isSuccess = ref(isCallback.value && state.value!.toLocaleLowerCase() === 'link-success')
const isFailure = ref(isCallback.value && state.value!.toLocaleLowerCase() === 'link-failed')
const isLinked = ref(!!currentUser.value?.discordUserId)
const hasAchievement = ref(isLinked.value && currentUser.value?.achievements.some((a) => a.uniqueIdentifier === 'certified_chirper'))

//#endregion

//#region hooks
onMounted(async () => {
  if (isCallback) {
    if (isSuccess.value) toast.success('Discord successfully linked!')
    else if (isFailure.value) toast.error('Sorry, something went wrong.')

    router.replace({ path: route.path })
  }
})
//#endregion

//#region methods
async function link() {
  const { url } = await DiscordService.getLinkUrl()
  window.location.href = url
}
//#endregion
</script>

<template>
  <ProfileSubsectionLayout title="Link Discord" :show-save="false">
    <div class="d-flex flex-column justify-content-center h-100 p-5">
      <VIcon prefix="brands" icon="discord" class="d-block text-center mb-2 mt-n5" style="font-size: 100px" />

      <template v-if="!isLinked">
        <button class="btn btn-primary" @click="link">Link Your Discord</button>
        <span class="text-stone-200 mt-3 d-block text-center px-3"
          ><i class="fw-bold">PSST!</i><br /><br /><br />Linking your discord <i>might</i> earn you a pretty cool achievement, and an exclusive banner
          and title.<br /><br /><br />But you didn't hear it from me...</span
        >
      </template>

      <template v-else>
        <button class="btn btn-primary" disabled>Discord Linked!</button>
        <span v-if="hasAchievement" class="text-stone-400 mt-3 d-block text-center px-4">
          You've earned the <span class="text-stone-0">Certified Chirper</span> achievement, and a fancy new title and banner to go along with it!
        </span>
        <span v-else class="mt-3 d-block text-center px-4">
          Looks like you haven't claimed your prize yet!
          <br /><br />
          Join the <a target="_blank" class="fw-bold" href="https://discord.gg/8xqnqs35">official Discord server</a> to get your achievement, banner,
          and title. <br /><br />
          <span class="text-stone-300"
            >If you are already in the server and seeing this message, try leaving and rejoining<br /><b>or</b><br />send the following message in the
            server:
            <pre class="text-stone-0 fs-6 my-2 bg-stone-900 py-1 rounded-md">/verify</pre>
          </span>
        </span>
      </template>
    </div>
  </ProfileSubsectionLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
</style>
