<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import VIcon from '@/components/VIcon.vue'
import VUser from '@/components/VUser.vue'
import { useFriendsStore } from '@/stores/friends'
import { useToast } from '@/composables/useToast'
import { getErrorMessage } from '@/helpers/requestHelpers'
import { friendLinkMessage, inviteSummary } from '@/helpers/friendHelpers'
import { useUserStore } from '@/stores/user'

/** Lobby invites, friend requests, friends, and finding more: your friend link and people you've played with. */
const friendsStore = useFriendsStore()
const { list } = storeToRefs(friendsStore)
const { currentUser } = storeToRefs(useUserStore())
const router = useRouter()
const toast = useToast()
const isLoading = ref(!list.value)
// The row being saved, so its buttons can't be pressed twice.
const busy = ref<string>()

onMounted(async () => {
  try {
    await friendsStore.load()
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't load your friends."))
  } finally {
    isLoading.value = false
  }
})

async function run(key: string, action: () => Promise<unknown>, failure: string, success?: string) {
  busy.value = key
  try {
    await action()
    if (success) toast.success(success, { timeout: 2000 })
  } catch (ex) {
    toast.error(getErrorMessage(ex, failure))
  } finally {
    busy.value = undefined
  }
}

const accept = (friend: Friend) => run(friend.id, () => friendsStore.add(friend.id), "Couldn't accept the request.", `You and ${friend.nickname} are now friends!`)
const decline = (friend: Friend) => run(friend.id, () => friendsStore.remove(friend.id), "Couldn't decline the request.")
const cancel = (friend: Friend) => run(friend.id, () => friendsStore.remove(friend.id), "Couldn't cancel the request.")

function unfriend(friend: Friend) {
  if (!confirm(`Remove ${friend.nickname} from your friends?`)) return
  run(friend.id, () => friendsStore.remove(friend.id), "Couldn't remove them.")
}

const addSuggested = (friend: Friend) => run(friend.id, () => friendsStore.add(friend.id), "Couldn't send the request.", `Friend request sent to ${friend.nickname}!`)

/** Shares a link to the user's profile, where others can add them; the share sheet on phones, else the clipboard. */
async function shareFriendLink() {
  const nickname = currentUser.value?.nickname
  if (!nickname) return
  const text = friendLinkMessage(window.location.origin, nickname)
  try {
    if (navigator.share) {
      await navigator.share({ text })
      return
    }
    await navigator.clipboard.writeText(text)
    toast.success('Copied your friend link!', { timeout: 2000 })
  } catch (ex) {
    // Closing the share sheet isn't a failure.
    if (ex instanceof DOMException && ex.name === 'AbortError') return
    toast.error('Cannot copy')
  }
}

const dismiss = (invite: LobbyInvite) => run(invite.joinCode, () => friendsStore.dismissInvite(invite.joinCode), "Couldn't dismiss the invite.")
const join = (invite: LobbyInvite) => router.push({ name: 'Lobby', params: { joinCode: invite.joinCode } })
const profile = (friend: Friend) => router.push(`/u/${encodeURIComponent(friend.nickname)}`)
</script>

<template>
  <HeaderLayout title="Friends" :show-save="false">
    <div v-if="isLoading" class="p-3">
      <div v-for="n in 6" :key="n" class="placeholder placeholder-wave w-100 rounded-md mb-2" style="height: 56px; opacity: 0.2"></div>
    </div>
    <div v-else-if="list" class="p-3">
      <button class="btn btn-stone-900 text-stone-0 fw-bold w-100 p-3 mb-4 d-flex align-items-center justify-content-center" @click="shareFriendLink">
        <VIcon prefix="sr" icon="share" class="me-2" />
        <span>Share My Friend Link</span>
      </button>

      <section v-if="list.invites.length" class="mb-4">
        <h2 class="section-title">Lobby Invites</h2>
        <div v-for="invite in list.invites" :key="invite.joinCode" class="row-card is-highlighted">
          <VUser display="avatar" :user="invite.sender" :avatar-size-in-px="40" />
          <div class="flex-grow-1 min-w-0">
            <div class="fw-bold text-truncate">{{ invite.sender.nickname }}</div>
            <div class="fs-7 text-stone-300 text-truncate">invited you to lobby {{ invite.joinCode }}</div>
            <div class="fs-7 text-stone-400 text-truncate">{{ inviteSummary(invite) }}</div>
          </div>
          <button class="btn btn-sm btn-primary fw-bold" :disabled="busy === invite.joinCode" @click="join(invite)">Join</button>
          <button class="btn btn-sm btn-link text-stone-400 px-1" :disabled="busy === invite.joinCode" title="Dismiss" aria-label="Dismiss invite" @click="dismiss(invite)">
            <VIcon prefix="sr" icon="cross-small" class="fs-4" />
          </button>
        </div>
      </section>

      <section v-if="list.incoming.length" class="mb-4">
        <h2 class="section-title">Friend Requests</h2>
        <div v-for="friend in list.incoming" :key="friend.id" class="row-card is-highlighted">
          <a role="button" @click="profile(friend)"><VUser display="avatar" :user="friend" :avatar-size-in-px="40" /></a>
          <a role="button" class="flex-grow-1 min-w-0 fw-bold text-truncate text-stone-0 text-decoration-none" @click="profile(friend)">{{ friend.nickname }}</a>
          <button class="btn btn-sm btn-primary fw-bold" :disabled="busy === friend.id" @click="accept(friend)">Accept</button>
          <button class="btn btn-sm btn-link text-stone-400" :disabled="busy === friend.id" @click="decline(friend)">Decline</button>
        </div>
      </section>

      <section class="mb-4">
        <h2 class="section-title">Friends</h2>
        <p v-if="!list.friends.length" class="text-stone-400 fs-7">
          No friends yet. Add people you've played with below, or share your friend link. Once they accept, you can invite
          them from a lobby's share button.
        </p>
        <div v-for="friend in list.friends" :key="friend.id" class="row-card">
          <a role="button" @click="profile(friend)"><VUser display="avatar" :user="friend" :avatar-size-in-px="40" /></a>
          <a role="button" class="flex-grow-1 min-w-0 fw-bold text-truncate text-stone-0 text-decoration-none" @click="profile(friend)">{{ friend.nickname }}</a>
          <button class="btn btn-sm btn-link text-stone-400" :disabled="busy === friend.id" title="Remove friend" @click="unfriend(friend)">
            <VIcon prefix="sr" icon="user-minus" />
          </button>
        </div>
      </section>

      <section v-if="list.outgoing.length" class="mb-4">
        <h2 class="section-title">Sent Requests</h2>
        <div v-for="friend in list.outgoing" :key="friend.id" class="row-card is-muted">
          <a role="button" @click="profile(friend)"><VUser display="avatar" :user="friend" :avatar-size-in-px="40" /></a>
          <a role="button" class="flex-grow-1 min-w-0 fw-bold text-truncate text-stone-0 text-decoration-none" @click="profile(friend)">{{ friend.nickname }}</a>
          <button class="btn btn-sm btn-link text-stone-400" :disabled="busy === friend.id" @click="cancel(friend)">Cancel</button>
        </div>
      </section>

      <section v-if="list.suggestions.length">
        <h2 class="section-title">People You've Played With</h2>
        <div v-for="friend in list.suggestions" :key="friend.id" class="row-card">
          <a role="button" @click="profile(friend)"><VUser display="avatar" :user="friend" :avatar-size-in-px="40" /></a>
          <a role="button" class="flex-grow-1 min-w-0 fw-bold text-truncate text-stone-0 text-decoration-none" @click="profile(friend)">{{ friend.nickname }}</a>
          <button class="btn btn-sm btn-primary fw-bold" :disabled="busy === friend.id" @click="addSuggested(friend)">
            <VIcon prefix="sr" icon="user-add" class="me-1" />Add
          </button>
        </div>
      </section>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.section-title {
  font-size: 12px;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: map-get($custom-colors, 'stone-400');
  margin-bottom: 8px;
}

.row-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  margin-bottom: 8px;
  border-radius: 12px;
  background-color: map-get($custom-colors, 'stone-800');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.row-card.is-highlighted {
  background-color: map-get($custom-colors, 'stone-700');
  border-color: map-get($custom-colors, 'stone-400');
  box-shadow: 0 0 3px rgba(map-get($custom-colors, 'primary'), 1);
}

.row-card.is-muted {
  opacity: 0.75;
}

.min-w-0 {
  min-width: 0;
}
</style>
