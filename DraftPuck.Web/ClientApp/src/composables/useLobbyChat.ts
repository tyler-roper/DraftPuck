import { ref } from 'vue'
import { useLobbyStore } from '@/stores/lobby'
import { parseAllDates } from '@/helpers/dateHelpers'
import { mentionsMember } from '@/helpers/chatHelpers'

/**
 * Incoming chat messages, and the unread and mention counts shown on the chat tab while the chat isn't open.
 * @param isChatOpen Whether the user can see the chat right now (messages arriving then aren't unread).
 */
export function useLobbyChat(isChatOpen: () => boolean) {
  const store = useLobbyStore()
  const unreadCount = ref(0)
  const unreadMentionCount = ref(0)

  /** Handles a message pushed by the server. Your own messages are already shown when you send them. */
  function onMessage(message: Message) {
    const me = store.currentMember
    if (message.lobbyMemberId === me?.id) return

    parseAllDates(message)
    store.addMessageToStore(message)

    if (isChatOpen()) return
    unreadCount.value++
    if (me && mentionsMember(message.message, me.name)) unreadMentionCount.value++
  }

  function markRead() {
    unreadCount.value = 0
    unreadMentionCount.value = 0
  }

  return { unreadCount, unreadMentionCount, onMessage, markRead }
}
