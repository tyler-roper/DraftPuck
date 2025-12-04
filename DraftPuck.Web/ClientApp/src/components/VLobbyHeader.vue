<script setup lang="ts">
import { computed } from 'vue'
import { addHours, isWithinInterval } from 'date-fns'
import VUser from '@/components/VUser.vue'

defineProps<{
  joinCode: string | undefined
}>()

const emit = defineEmits<{
  copyInvite: []
  openInstructions: []
  openNotificationSettings: []
}>()

const is4Nations = computed(() => {
  const today = new Date()
  const firstDay = new Date(2025, 1, 10)
  const lastDay = new Date(2025, 1, 20)
  const paddingHours = 12
  const start = addHours(firstDay, -1 * paddingHours)
  const end = addHours(lastDay, paddingHours)

  return isWithinInterval(today, { start, end })
})
</script>

<template>
  <div class="bg-stone-900 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center justify-content-between" style="z-index: 10">
    <router-link :to="{ name: 'Home' }" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
      <img v-if="!is4Nations" src="/img/logo-wide.png" />
      <img v-if="is4Nations" src="/img/logo-wide-4nations.png" />
    </router-link>

    <a
      @click="emit('copyInvite')"
      role="button"
      class="d-block ms-auto bg-primary text-stone-900 px-2 rounded text-decoration-none fs-7 ms-1 d-flex align-items-center"
      v-if="joinCode !== undefined"
    >
      <span class="fs-5 me-1 fw-bold" style="letter-spacing: 3px">{{ joinCode }}</span>
      <i class="fi fi-sr-share d-block mb-n1 d-block"></i>
    </a>

    <a
      class="d-flex ms-auto me-sm-5 me-3 pt-1 text-white fw-bold text-decoration-none align-items-center"
      role="button"
      @click="emit('openNotificationSettings')"
    >
      <i class="fi fi-rr-settings d-block fs-3" style="line-height: 20px"></i>
      <span class="d-none d-sm-block text-uppercase ms-2" style="margin-top: -2px">Notifications</span>
    </a>

    <a class="d-flex pt-1 text-stone-0 fw-bold text-decoration-none align-items-center" role="button" @click="emit('openInstructions')">
      <i class="fi fi-rr-question-square d-block fs-3" style="line-height: 20px"></i>
      <span class="d-none d-sm-block text-uppercase ms-2" style="margin-top: -2px">How To Play</span>
    </a>

    <VUser class="ms-3" display="avatar" :avatar-size-in-px="30" :show-menu-on-click="true" />
  </div>
</template>

<style scoped lang="scss">
.banner-logo img {
  height: 40px;
  width: auto;
  max-width: 100%;
  object-fit: contain;
}
</style>
