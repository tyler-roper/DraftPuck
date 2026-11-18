import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import FriendService from '@/services/FriendService'
import { pendingCount } from '@/helpers/friendHelpers'

/**
 * The signed-in user's friends, friend requests and lobby invites. Guests have none, so the app only loads this for
 * a signed-in user (see App.vue), and clears it on sign-out.
 */
export const useFriendsStore = defineStore('friends', () => {
  const list = ref<FriendsList>()
  let loading: Promise<void> | undefined

  /** Friend requests to answer plus lobby invites: the number on the badge. */
  const pending = computed(() => pendingCount(list.value))

  /** Fetches the lists. Calls made while a fetch is running share it. */
  function load(): Promise<void> {
    loading ??= FriendService.getFriends()
      .then((result) => {
        list.value = result
      })
      .finally(() => {
        loading = undefined
      })
    return loading
  }

  function reset() {
    list.value = undefined
  }

  /** Sends a friend request, or accepts theirs. */
  async function add(userId: string) {
    await FriendService.addFriend(userId)
    await load()
  }

  /** Unfriends, or declines or cancels a request. */
  async function remove(userId: string) {
    await FriendService.removeFriend(userId)
    await load()
  }

  async function dismissInvite(joinCode: string) {
    await FriendService.dismissLobbyInvite(joinCode)
    forgetInvite(joinCode)
  }

  /**
   * The user is in this lobby now (however they joined), so its invite is answered. The server already leaves it out
   * of the next load; this drops it from the badge and the list straight away.
   */
  function forgetInvite(joinCode: string) {
    const code = joinCode.toUpperCase()
    if (list.value?.invites.some((i) => i.joinCode.toUpperCase() === code))
      list.value = { ...list.value, invites: list.value.invites.filter((i) => i.joinCode.toUpperCase() !== code) }
  }

  return { list, pending, load, reset, add, remove, dismissInvite, forgetInvite }
})
