<script setup lang="ts">
import VIcon from '@/components/VIcon.vue';
withDefaults(defineProps<{
  prefix?: string;
  icon?: string;
  padding?: boolean;
  small?: boolean;
}>(), {
  prefix: 'rr',
  padding: true,
  small: false
});

</script>

<template>
  <div class="input-group" :class="{'small-input': small}">
    <div v-if="icon" class="input-icon" :class="{ 'left-0': !padding }">
      <VIcon :icon="icon" :prefix="prefix"></VIcon>
    </div>
    <slot></slot>
  </div>
</template>

<style scoped>
  .left-0 {
    left: 0 !important;
  }
</style>