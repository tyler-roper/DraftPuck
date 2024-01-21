<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue: any
  id: string
}>()
const emit = defineEmits(['update:modelValue'])

const value = computed({
  get() {
    return props.modelValue
  },
  set(value: boolean) {
    emit('update:modelValue', value)
  }
})
</script>

<template>
  <div class="form-check form-switch">
    <input class="form-check-input" type="checkbox" :id="id" role="switch" v-model="value" checked />
    <label class="form-check-label" :for="id">
      <slot></slot>
    </label>
  </div>
</template>

<style scoped>
.form-check-input:not(:checked) {
  background-color: white;
}
</style>
