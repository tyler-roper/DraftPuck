<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import VUser from '@/components/VUser.vue'
import { useUserStore } from '@/stores/user'
import { useFriendsStore } from '@/stores/friends'
import { useToast } from '@/composables/useToast'
import FriendService from '@/services/FriendService'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { inviteOptions } from '@/helpers/friendHelpers'

/** Sharing a lobby: copy its link, or (signed in) pick friends and send them an invite. */
const props = defineProps<{ joinCode: string; memberUserIds: string[] }>()
const emit = defineEmits(['close'])

const { isLoggedIn } = storeToRefs(useUserStore())
const friendsStore = useFriendsStore()
const { list } = storeToRefs(friendsStore)
const toast = useToast()

const isLoading = ref(true)
const isSending = ref(false)
const invited = ref<string[]>([])
const selected = ref<string[]>([])
const options = computed(() => inviteOptions(list.value?.friends ?? [], props.memberUserIds, invited.value))

const stopPropagation = (e: Event) => e.stopPropagation()
const close = () => {
  if (!isSending.value) emit('close')
}

onMounted(async () => {
  if (!isLoggedIn.value) {
    isLoading.value = false
    return
  }
  try {
    const [, invitedIds] = await Promise.all([friendsStore.load(), FriendService.getLobbyInvites(props.joinCode)])
    invited.value = invitedIds
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't load your friends."))
  } finally {
    isLoading.value = false
  }
})

async function copyLink() {
  try {
    await navigator.clipboard.writeText(`Join my DRAFTPUCK lobby! Code: ${props.joinCode}\n\nhttps://draftpuck.com/lobby/${props.joinCode}`)
    toast.success('Copied invite to clipboard!', { timeout: 2000 })
  } catch {
    toast.error('Cannot copy')
  }
}

function toggle(friendId: string) {
  selected.value = selected.value.includes(friendId) ? selected.value.filter((id) => id !== friendId) : [...selected.value, friendId]
}

async function sendInvites() {
  if (!selected.value.length) return
  isSending.value = true
  try {
    const count = selected.value.length
    invited.value = await FriendService.sendLobbyInvites(props.joinCode, selected.value)
    selected.value = []
    toast.success(count === 1 ? 'Invite sent!' : `${count} invites sent!`, { timeout: 2000 })
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't send the invites."))
  } finally {
    isSending.value = false
  }
}

const stateText = { invited: 'Invited', member: 'In lobby' }
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click="stopPropagation">
      <div class="modal-header justify-content-between">
        <span class="d-block">Invite to {{ joinCode }}</span>
        <a v-if="!isSending" role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>
      <div class="modal-body dark">
        <button class="btn btn-stone-900 text-stone-0 fw-bold w-100 p-3 d-flex align-items-center justify-content-center" @click="copyLink">
          <VIcon icon="copy-alt" class="me-2" />
          <span>Copy Invite Link</span>
        </button>

        <div class="text-uppercase fw-bold fs-7 text-stone-400 mt-4 mb-2">Invite Friends</div>
        <p v-if="!isLoggedIn" class="text-stone-400 fs-7 mb-0">
          <router-link :to="{ name: 'Join', query: { lobby: joinCode } }" @click="emit('close')">Sign up</router-link>
          to add friends and invite them to your lobbies.
        </p>
        <div v-else-if="isLoading">
          <div v-for="n in 3" :key="n" class="placeholder placeholder-wave w-100 rounded-md mb-2" style="height: 48px; opacity: 0.2"></div>
        </div>
        <p v-else-if="!options.length" class="text-stone-400 fs-7 mb-0">
          No friends yet. Add people from their profile (tap a member, then View Profile), and they'll show up here.
        </p>
        <template v-else>
          <div class="friend-list">
            <label
              v-for="{ friend, state } in options"
              :key="friend.id"
              class="friend-row"
              :class="{ 'is-disabled': state !== 'available', 'is-selected': selected.includes(friend.id) }"
            >
              <input
                type="checkbox"
                class="form-check-input m-0"
                :disabled="state !== 'available'"
                :checked="state !== 'available' || selected.includes(friend.id)"
                @change="toggle(friend.id)"
              />
              <VUser display="avatar" :user="friend" :avatar-size-in-px="36" />
              <span class="flex-grow-1 text-truncate fw-bold">{{ friend.nickname }}</span>
              <span v-if="state !== 'available'" class="fs-8 text-uppercase text-stone-400">{{ stateText[state] }}</span>
            </label>
          </div>
          <VButton
            class="btn btn-primary fw-bold w-100 p-3 mt-3 text-uppercase"
            :is-loading="isSending"
            :disabled="!selected.length"
            @click="sendInvites"
          >
            Send Invite{{ selected.length > 1 ? `s (${selected.length})` : '' }}
          </VButton>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.modal-body.dark {
  color: map-get($custom-colors, 'stone-0');
  background: map-get($custom-colors, 'stone-850');
}

.friend-list {
  max-height: 45vh;
  overflow-y: auto;
  border-radius: 12px;
  background: map-get($custom-colors, 'stone-900');
}

.friend-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  cursor: pointer;
  border-bottom: 1px solid map-get($custom-colors, 'stone-800');
}

.friend-row:last-child {
  border-bottom: none;
}

.friend-row.is-selected {
  background: rgba(map-get($custom-colors, 'primary'), 0.12);
}

.friend-row.is-disabled {
  cursor: default;
  opacity: 0.6;
}
</style>
