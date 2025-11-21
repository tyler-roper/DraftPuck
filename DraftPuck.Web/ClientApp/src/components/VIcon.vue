<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    icon: string
    prefix?: string
  }>(),
  {
    prefix: 'rr'
  }
)
</script>

<template>
  <i :class="`fi fi-${props.prefix}-${props.icon}`"></i>
</template>

<style scoped>
i.fi::before {
  display: inline-block;
  transform: translateY(15%);
}
</style>
