<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { DRINK_MESSAGE_MAX_LENGTH, DRINK_MESSAGE_SUGGESTIONS, tidyDrinkMessage } from '@/helpers/drinkMessageHelpers'

/** Gives a drink with a message: type one, or tap a suggestion. */
const props = defineProps<{ recipientName: string; isGiving?: boolean }>()
const emit = defineEmits<{ give: [message: string | undefined]; close: [] }>()

const message = ref('')
const input = ref<HTMLInputElement>()
const remaining = computed(() => DRINK_MESSAGE_MAX_LENGTH - message.value.length)

onMounted(async () => {
  await nextTick()
  input.value?.focus()
})

function give() {
  if (props.isGiving) return
  emit('give', tidyDrinkMessage(message.value))
}
</script>

<template>
  <div class="modal-overlay" @click="emit('close')">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block text-truncate">🍺 Give {{ recipientName }} a drink</span>
        <a v-if="!isGiving" role="button" class="text-uppercase text-primary text-decoration-none ms-3" @click="emit('close')">Close</a>
      </div>
      <form class="modal-body dark" @submit.prevent="give">
        <label for="drink-message" class="form-label fw-bold fs-7 text-uppercase text-stone-400">Add a message (optional)</label>
        <input id="drink-message" ref="input" v-model="message" class="form-control" type="text" autocomplete="off"
          :maxlength="DRINK_MESSAGE_MAX_LENGTH" placeholder="That's for the Oilers goal." enterkeyhint="send" />
        <div class="d-flex justify-content-end fs-8 text-stone-400 mt-1">
          <span :class="{ 'text-danger': remaining <= 10 }">{{ remaining }}</span>
        </div>

        <div class="d-flex flex-wrap gap-2 mt-2">
          <button v-for="suggestion in DRINK_MESSAGE_SUGGESTIONS" :key="suggestion" type="button" class="chip"
            :class="{ chosen: message === suggestion }" @click="message = suggestion">
            {{ suggestion }}
          </button>
        </div>

        <button type="submit" class="btn btn-primary fw-bold w-100 mt-3 py-2" :disabled="isGiving">
          {{ isGiving ? 'Giving…' : `Give ${recipientName} a drink` }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Dark, like the rest of the app (the shared modal body is white).
.modal-body.dark {
  background: map-get($custom-colors, 'stone-850');
  color: map-get($custom-colors, 'stone-0');
}

.chip {
  padding: 4px 12px;
  font-size: 0.85rem;
  font-weight: 700;
  border-radius: 999px;
  border: 1px solid map-get($custom-colors, 'stone-600');
  background: map-get($custom-colors, 'stone-800');
  color: map-get($custom-colors, 'stone-100');
}

.chip.chosen {
  border-color: map-get($custom-colors, 'amber-500');
  background: map-get($custom-colors, 'amber-500');
  color: map-get($custom-colors, 'stone-900');
}
</style>
