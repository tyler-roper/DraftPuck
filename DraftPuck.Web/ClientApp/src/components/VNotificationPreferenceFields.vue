<script setup lang="ts" generic="K extends NotificationKey">
import type NotificationPreference from '@/enums/notificationPreference'
import { notificationSettings, type NotificationKey } from '@/helpers/notificationSettings'
import { computed } from 'vue'

/** Radio-button questions for the given notification settings, bound with v-model. */
const props = defineProps<{
  keys: K[]
  modelValue: Record<K, NotificationPreference>
  idPrefix: string
  /** Dark cards, for dark pages (Account Settings); light by default, for the white lobby modal. */
  dark?: boolean
}>()
const emit = defineEmits<{ 'update:modelValue': [value: Record<K, NotificationPreference>] }>()

const settings = computed(() => notificationSettings.filter((s) => (props.keys as NotificationKey[]).includes(s.key)))

function select(key: NotificationKey, value: NotificationPreference) {
  emit('update:modelValue', { ...props.modelValue, [key]: value })
}
</script>

<template>
  <div class="notification-form-container" :class="{ dark }">
    <div v-for="setting in settings" :key="setting.key" class="question">
      <span class="question-text">{{ setting.question }}</span>
      <template v-for="option in setting.options" :key="option.value">
        <input
          type="radio"
          :id="`${idPrefix}-${setting.key}-${option.value}`"
          :name="`${idPrefix}-${setting.key}`"
          :checked="modelValue[setting.key as K] === option.value"
          @change="select(setting.key, option.value)"
        />
        <label :for="`${idPrefix}-${setting.key}-${option.value}`">{{ option.label }}</label>
      </template>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
// The questions are light cards, so they set their own dark text instead of inheriting the page's (white on dark pages).
.notification-form-container {
  margin-bottom: -0.5rem;
  color: map-get($custom-colors, 'stone-900');
}

.notification-form-container .question {
  padding: 0.5rem;
  background: map-get($custom-colors, 'stone-100');
  border-radius: 0.5rem;
}

.notification-form-container .question:not(:last-child) {
  padding-bottom: 0.75rem;
  margin-bottom: 0.75rem;
}

.notification-form-container .question-text {
  font-weight: bold;
  display: block;
}

.notification-form-container input[type='radio'] {
  display: none;
}

.notification-form-container label {
  padding: 0.25rem 0.5rem;
  margin: 0.5rem 0;
  border-radius: 5px;
  background: map-get($custom-colors, 'stone-100');
  display: block;
  width: 100%;
  opacity: 0.5;
}

.notification-form-container label:last-child {
  margin-bottom: 0;
}

.notification-form-container label::before {
  content: '';
  display: inline-block;
  width: 15px;
  height: 15px;
  border-radius: 20px;
  margin-bottom: -3px;
  background-color: map-get($custom-colors, 'stone-800');
  margin-right: 0.5rem;
  border: 2px solid transparent;
}

.notification-form-container input[type='radio']:checked + label {
  font-weight: bold;
  opacity: 1;
}

.notification-form-container input[type='radio']:checked + label::before {
  background: map-get($custom-colors, 'stone-0');
  border: 4px solid map-get($custom-colors, 'primary');
}

// Dark: cards like the rest of a dark page, with light text.
.notification-form-container.dark {
  color: map-get($custom-colors, 'stone-0');
}

.notification-form-container.dark .question {
  background: map-get($custom-colors, 'stone-900');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.notification-form-container.dark label {
  background: map-get($custom-colors, 'stone-800');
}

.notification-form-container.dark label::before {
  background-color: map-get($custom-colors, 'stone-600');
}
</style>
