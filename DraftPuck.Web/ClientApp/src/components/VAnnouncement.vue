<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import VIcon from '@/components/VIcon.vue'
import { dismissAnnouncement, isAnnouncementVisible } from '@/helpers/announcement'

/**
 * A one-off "we've made some big changes" card on the home page, opening the release notes. Opening it or dismissing
 * it hides it for good; the release notes stay a link away at the bottom of the home page.
 */
const router = useRouter()
const isVisible = ref(isAnnouncementVisible())

function hide() {
  isVisible.value = false
  dismissAnnouncement()
}

function open() {
  hide()
  router.push({ name: 'ReleaseNotes' })
}
</script>

<template>
  <div v-if="isVisible" class="announcement">
    <button class="open" @click="open">
      <VIcon prefix="sr" icon="sparkles" class="fs-4 me-3 text-primary" />
      <span class="d-block text-start min-w-0 flex-grow-1">
        <span class="d-block title">We've made some big changes</span>
        <span class="d-block fs-7 text-stone-300">Friends, post-game awards, a new feed and more. See what's new.</span>
      </span>
      <VIcon prefix="sr" icon="angle-right" class="fs-5 ms-2 text-stone-400" />
    </button>
    <button class="dismiss" title="Dismiss" aria-label="Dismiss" @click="hide">
      <VIcon prefix="sr" icon="cross-small" class="fs-4" />
    </button>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.announcement {
  display: flex;
  align-items: stretch;
  border-radius: 15px;
  overflow: hidden;
  background-color: map-get($custom-colors, 'stone-900');
  border: 2px solid map-get($custom-colors, 'primary');
}

.open,
.dismiss {
  border: none;
  background: transparent;
  color: map-get($custom-colors, 'stone-0');
}

.open {
  flex-grow: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  padding: 12px 8px 12px 16px;
}

.dismiss {
  padding: 0 14px;
  color: map-get($custom-colors, 'stone-400');
  border-left: 1px solid map-get($custom-colors, 'stone-700');
}

.title {
  font-weight: bold;
  text-transform: uppercase;
}

.min-w-0 {
  min-width: 0;
}
</style>
