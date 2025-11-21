<script setup lang="ts" generic="TValue">
import { ref, computed, getCurrentInstance } from 'vue'

const props = defineProps<{
  modelValue: any
  options: Array<{ value: TValue; label: string }>
}>()
const emit = defineEmits(['update:modelValue'])

const value = computed({
  get() {
    return props.modelValue
  },
  set(value: TValue) {
    emit('update:modelValue', value)
  }
})

const componentId = getCurrentInstance()?.uid
const selectedIndex = computed(() => props.options.findIndex((o) => o.value === value.value))
const sliderStyle = computed(() => {
  const offset = selectedIndex.value * 100
  return {
    width: `calc(${100 / props.options.length}% - 10px)`,
    transform: `translateX(calc(${offset}% + ${offset / 10}px))`
  }
})
</script>

<template>
  <div class="switches-container">
    <div class="slider" :style="sliderStyle"></div>
    <div class="item-container">
      <template v-for="(option, idx) in options" :key="idx">
        <input type="radio" :id="`option-${componentId}-${idx}`" :value="option.value" v-model="value" />
        <label :for="`option-${componentId}-${idx}`">{{ option.label }}</label>
      </template>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

input[type='radio'] {
  display: none;
}

.switches-container {
  border-radius: 20px;
  background-color: map-get($custom-colors, 'stone-900');
  position: relative;
  display: flex;
  align-items: stretch;
}

.switches-container > .item-container {
  display: flex;
  align-items: center;
  width: 100%;
  z-index: 2;
}

.item-container > label {
  display: block;
  padding: 17px 0;
  flex: 1 1;
  text-align: center;
  color: map-get($custom-colors, 'stone-500');
  font-size: 12px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.item-container > input:checked + label {
  color: map-get($custom-colors, 'stone-1000');
}

.item-container > label:first-of-type {
  margin-left: 0;
}

.item-container > label:last-child {
  margin-right: 0;
}

.slider {
  position: absolute;
  border-radius: 16px;
  border: 5px solid transparent;
  background-color: map-get($custom-colors, 'stone-200');
  height: calc(100% - 10px);
  top: 5px;
  left: 5px;
  transition: transform 0.1s;
}
</style>
