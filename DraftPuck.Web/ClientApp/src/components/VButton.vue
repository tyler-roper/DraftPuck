<script setup lang="ts">
export interface Props {
  isLoading?: boolean
  showText?: boolean
  loadingText?: string
  type?: 'button' | 'submit' | 'reset'
}

withDefaults(defineProps<Props>(), {
  isLoading: false,
  showText: false,
  loadingText: 'Loading...',
  type: 'button'
})
</script>

<template>
  <button class="btn" :type="type" :disabled="isLoading">
    <slot v-if="!isLoading"></slot>
    <span v-if="isLoading">
      <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
      <span class="visually-hidden" role="status">Loading...</span>
      <span v-if="showText" role="status" class="d-inline-block ms-2">{{ loadingText }}</span>
    </span>
  </button>
</template>
