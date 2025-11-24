<script setup lang="ts">
defineProps<{
  message: string;
  title?: string;
}>();
</script>

<template>
  <div class="Vue-Toastification__toast-body">
    <span v-if="title" class="d-block fw-bold text-uppercase fs-7">{{ title }}</span>
    <div v-html="message"></div>
  </div>
</template>