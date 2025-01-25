<script setup lang="ts">
import type MessageViewModel from '@/models/messageViewModel'
import { addSeconds, format, parseISO } from 'date-fns'
import { useLobbyStore } from '@/stores/lobby'
import { storeToRefs } from 'pinia'
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import type SystemMessageViewModel from '@/models/systemMessageViewModel'

//const
const SECONDS_BETWEEN_MESSAGES = 3

//props
const props = withDefaults(
  defineProps<{
    messages: (MessageViewModel | SystemMessageViewModel)[]
  }>(),
  {
    messages: () => []
  }
)

//emitters
const emit = defineEmits(['command'])

//data
const lobbyStore = useLobbyStore()
const { currentUserId, isLobbyAdmin, debugLevel } = storeToRefs(lobbyStore)
const { sendMessage: storeSendMessage, setDebugging, sendDebugMessage, sendSystemMessage } = lobbyStore
const messageInput = ref<HTMLTextAreaElement | null>(null)
const messagesContainer = ref<HTMLDivElement | null>(null)
const message = ref('')
const error = ref<string>()
const lastSentMessage = ref(new Date(-1))
const isLockedToBottom = ref(true)
const errorTimer = ref<number>()



//hooks/methods
onMounted(async () => {
  focus()

  if (!messagesContainer.value) return
  messagesContainer.value.addEventListener('scroll', onChatScroll)
})

function processCommand(message: string) {
  const messageParts = message.split('/')
  if (messageParts.length <= 1) return

  message = message.toLowerCase()
  const commandParts = messageParts[1].split(' ')
  const command = commandParts[0]
  const args = commandParts.slice(1)

  emit('command', command, ...args)
}

function formatAsTime(date: Date | string) {
  date = typeof date === 'string' ? parseISO(date) : date
  return format(date, 'p')
}

function isCurrentMember(message: MessageViewModel | SystemMessageViewModel) {
  if (message.isSystem) return false
  return (message as MessageViewModel).lobbyMemberUserId === currentUserId.value
}

async function resizeMessageInput() {
  if (!messageInput.value) return
  messageInput.value.style.height = 0 + 'px'
  messageInput.value.style.height = messageInput.value?.scrollHeight + 'px'
}

function onEnterKeydown(e: KeyboardEvent) {
  if (e.shiftKey) return
  sendMessage(e)
}

async function sendMessage(e: Event, isSystem: boolean = false) {
  e.preventDefault()
  const originalMessage = message.value
  const isCommand = originalMessage.startsWith('/')

  if (isCommand) {
    try {
    processCommand(message.value)
    } catch (e) {
      if (typeof e === 'string') error.value = e
      return
    }
  }

  const wait = Math.ceil((Number(addSeconds(lastSentMessage.value, SECONDS_BETWEEN_MESSAGES)) - Number(new Date())) / 1000)

  window.clearTimeout(errorTimer.value)
  errorTimer.value = window.setTimeout(() => (error.value = ''), wait * 1000)

  if (wait > 0 && !isLobbyAdmin.value && !isSystem) return (error.value = `Wait ${wait} seconds.`)
  if (message.value.length > 400 && !isSystem) return (error.value = 'Message exceeds 400 characters.')
  if (!message.value.trim().length) return (error.value = 'Invalid message.')

  message.value = ''
  await nextTick()
  resizeMessageInput()

  try {
    if (!isCommand) {
      await storeSendMessage(originalMessage.trim())
      lastSentMessage.value = new Date()
    }
    error.value = ''
  } catch (exception) {
    message.value = originalMessage
    await nextTick()
    resizeMessageInput()

    
  }
}

function onChatScroll() {
  if (!messagesContainer.value) return

  const height = messagesContainer.value.getBoundingClientRect().height
  const scrollTop = messagesContainer.value.scrollTop
  const scrollHeight = messagesContainer.value.scrollHeight
  const amountScrolled = scrollHeight - scrollTop - height

  if (isLockedToBottom.value && Math.abs(amountScrolled) > 100) isLockedToBottom.value = false
  else if (!isLockedToBottom.value && Math.abs(amountScrolled) < 25) isLockedToBottom.value = true
}

async function scrollToBottom() {
  if (!messagesContainer.value) return
  await nextTick()
  messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
}

async function focus() {
  messageInput.value?.focus()
  scrollToBottom()
}

//watchers
watch(
  () => props.messages,
  async () => {
    if (isLockedToBottom.value) scrollToBottom()
  }
)

//expose
defineExpose({ focus })
</script>

<template>
  <div style="overflow-y: scroll" class="bg-stone-200 d-flex flex-column">
    <div
      class="bg-stone-150 py-2 px-3 ls-2 d-flex align-items-center"
      style="z-index: 2; position: sticky; top: 0; border-bottom: 1px solid rgba(0, 0, 0, 0.1)"
    >
      <div class="fs-6 me-2">💬</div>
      <div>
        <div class="text-decoration-none text-stone-900 d-flex align-items-center">
          <span class="fw-bold fs-6 text-uppercase">Chat</span>
        </div>
      </div>
    </div>
    <div ref="messagesContainer" class="flex-grow-1 flex-shrink-1 overflow-auto">
      <div class="message" v-for="(message, idx) in messages" :key="idx" :class="{ 'is-current-member': isCurrentMember(message) }">
        <span class="text-nowrap">
          <span class="text-stone-400 small">{{ formatAsTime(message.sent) }}</span>
          <span class="name fw-bold ms-2" v-if="!message.isSystem">{{ (message as MessageViewModel).lobbyMemberName }}:</span>
        </span>
        <span v-if="!message.isSystem" class="text-break ms-2">{{ message.message }}</span>
        <pre v-if="message.isSystem" class="text-break mb-0">{{ message.message }}</pre>
      </div>
    </div>
    <form @submit="sendMessage" class="chat-form position-relative">
      <a role="button" v-if="!isLockedToBottom" class="d-block lock-to-bottom btn btn-stone-900" @click="scrollToBottom"
        >Back to bottom <i class="fi fi-rr-arrow-down"></i
      ></a>
      <textarea
        v-model="message"
        maxlength="500"
        ref="messageInput"
        placeholder="Send a message"
        @input="resizeMessageInput"
        @keydown.enter="onEnterKeydown"
      ></textarea>
      <div class="d-flex align-items-center justify-content-end">
        <span class="d-block text-danger me-2 fw-bold">{{ error }}</span>
        <button class="d-block btn btn-primary fw-bold">Send</button>
      </div>
    </form>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.message {
  background: map-get($custom-colors, 'stone-0');
  padding: 4px 10px;
  color: map-get($custom-colors, 'stone-900');
  padding-left: 5px;
  border-left: 5px solid transparent;
}

.message.is-current-member .name {
  color: map-get($custom-colors, 'blue');
}

.chat-form {
  padding: 10px;
  background-color: map-get($custom-colors, 'stone-300');
}

textarea {
  padding: 10px;
  border: 1px solid map-get($custom-colors, 'stone-400');
  border-radius: 8px;
  width: 100%;
  resize: none;
  overflow-y: hidden;
  height: 39px;
}

a.lock-to-bottom {
  display: block;
  position: absolute;
  top: -50px;
  width: 90%;
  left: 50%;
  transform: translateX(-50%);
  opacity: 0.9;
}
</style>
