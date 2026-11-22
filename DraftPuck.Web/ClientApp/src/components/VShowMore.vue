<script setup lang="ts" generic="T">
import { computed, ref } from 'vue'

/** A list that shows its first few items, with View more / View less. Key it to start collapsed again. */
const props = withDefaults(
  defineProps<{
    items: T[]
    shown?: number
  }>(),
  { shown: 3 }
)

defineSlots<{ default(props: { item: T; index: number }): unknown }>()

const isExpanded = ref(false)
const visible = computed(() => (isExpanded.value ? props.items : props.items.slice(0, props.shown)))
</script>

<template>
  <template v-for="(item, index) in visible" :key="index">
    <slot :item="item" :index="index" />
  </template>
  <button v-if="items.length > shown" type="button" class="more" @click="isExpanded = !isExpanded">
    {{ isExpanded ? 'View less' : `View more (${items.length - shown})` }}
  </button>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.more {
  display: block;
  margin: 6px auto 0;
  padding: 4px 10px;
  border: 0;
  background: none;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: map-get($custom-colors, 'primary');
}
</style>
