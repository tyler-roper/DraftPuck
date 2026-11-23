<script setup lang="ts">
import { ref } from 'vue'
import { useToast } from '@/composables/useToast'
import { useGiveDrinks } from '@/composables/useGiveDrinks'
import { getErrorMessage } from '@/helpers/requestHelpers'
import VDrinkMessageModal from '@/components/VDrinkMessageModal.vue'

/** Who drinks: one tap hands your next drink to someone, or 💬 adds a message first. */
const toast = useToast()
const { pendingDrinks, recipients, closeSheet, give } = useGiveDrinks()

const messageRecipient = ref<LobbyMember>()
const isGiving = ref(false)

async function giveTo(recipient: LobbyMember, message?: string) {
  if (isGiving.value) return
  isGiving.value = true
  try {
    await give(recipient, message)
    messageRecipient.value = undefined
  } catch (error) {
    toast.error(getErrorMessage(error, `Couldn't give ${recipient.name} a drink. Please try again.`))
  } finally {
    isGiving.value = false
  }
}
</script>

<template>
  <VDrinkMessageModal
    v-if="messageRecipient"
    :recipient-name="messageRecipient.name"
    :is-giving="isGiving"
    @give="(message) => giveTo(messageRecipient!, message)"
    @close="messageRecipient = undefined"
  />

  <div v-else class="modal-overlay" @click="!isGiving && closeSheet()">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block text-truncate"
          >🍺 Give a drink <span class="left">{{ pendingDrinks.length }} left</span></span
        >
        <a v-if="!isGiving" role="button" class="text-uppercase text-primary text-decoration-none ms-3" @click="closeSheet">Close</a>
      </div>

      <div class="modal-body dark">
        <p v-if="recipients.length === 0" class="mb-0 text-stone-300">Nobody to give one to yet. Share the lobby code to get people in.</p>
        <p v-else class="mb-2 fs-7 text-stone-300">Tap who drinks.</p>

        <div v-for="recipient in recipients" :key="recipient.id" class="recipient">
          <button type="button" class="give" :disabled="isGiving" @click="giveTo(recipient)">
            <span class="me-2">🍺</span><span class="name">{{ recipient.name }}</span>
          </button>
          <button
            type="button"
            class="with-message"
            :disabled="isGiving"
            :title="`Give ${recipient.name} a drink with a message`"
            :aria-label="`Give ${recipient.name} a drink with a message`"
            @click="messageRecipient = recipient"
          >
            💬
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Dark, like the rest of the app (the shared modal body is white).
.modal-body.dark {
  background: map-get($custom-colors, 'stone-850');
  color: map-get($custom-colors, 'stone-0');
  max-height: 65vh;
  overflow-y: auto;
}

.left {
  margin-left: 6px;
  padding: 1px 8px;
  border-radius: 999px;
  font-size: 0.8rem;
  background: map-get($custom-colors, 'primary');
  color: map-get($custom-colors, 'stone-900');
}

.recipient {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.recipient:last-child {
  margin-bottom: 0;
}

.give,
.with-message {
  border-radius: 12px;
  border: 1px solid map-get($custom-colors, 'stone-700');
  background: map-get($custom-colors, 'stone-800');
  color: map-get($custom-colors, 'stone-0');
}

.give {
  flex-grow: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  padding: 12px 14px;
  font-weight: 700;
  text-align: left;
}

.give .name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.with-message {
  flex: none;
  width: 52px;
  font-size: 1.1rem;
}

.give:hover:not(:disabled),
.with-message:hover:not(:disabled) {
  border-color: map-get($custom-colors, 'primary');
}
</style>
