<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue: boolean
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
  background-color: #44403c;
  border-color: #57534e;
  --bs-form-switch-bg: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='-4 -4 8 8'%3e%3ccircle r='3' fill='%23a8a29e'/%3e%3c/svg%3e");
}
</style>
