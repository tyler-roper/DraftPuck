import { computed, ref, toRaw } from 'vue'
import { defineStore } from 'pinia'
import type Bot from '@/models/bot'
import LobbyService from '@/services/LobbyService'
import { compareAsc } from 'date-fns'
import type PickRequest from '@/models/pickRequest'
import SystemMessageViewModel from '@/models/systemMessageViewModel'
import MessageViewModel from '@/models/messageViewModel'
import { useUserStore } from '@/stores/user'
import { getErrorMessage } from '@/helpers/requestHelpers'

export const useLobbyStore = defineStore('lobby', () => {
  //#region state
  const lobby = ref<Lobby>()
  const events = ref<LobbyEvent[]>([])
  const systemMessages = ref<SystemMessageViewModel[]>([])
  const debugLevel = ref(0)

  const userStore = useUserStore()
  const currentUserId = computed(() => userStore.currentUser?.id)
  //#endregion

  //#region mutations
  const setLobby = (_lobby: Lobby) => (lobby.value = _lobby)
  const setEvents = (_events: LobbyEvent[]) => (events.value = _events)

  function addPick(pick: LobbyMemberPick) {
    const member = lobby.value?.members.find((m) => pick.lobbyMemberId === m.id)
    // A lobby refetch (e.g. from someone else's event) may already have brought this pick in.
    if (!member || member.picks.some((p) => p.id === pick.id)) return
    member.picks.push(pick)
  }

  function removePickFromStore(pickId: string) {
    if (!lobby.value?.members) return
    for (const member of lobby.value.members) {
      const pickIdx = member.picks.findIndex((p) => p.id === pickId)
      if (pickIdx === -1) continue

      member.picks.splice(pickIdx, 1)
    }
  }

  function setDrinkRecipient(drink: Drink) {
    if (!lobby.value?.members) return

    for (const member of lobby.value.members) {
      const drinkToUpdate = member.picks.flatMap((p) => p.drinks).find((d) => d.id === drink.id)
      if (drinkToUpdate) return (drinkToUpdate.recipientLobbyMemberId = drink.recipientLobbyMemberId)
    }
  }

  function addLobbyEvent(event: LobbyEvent) {
    events.value.push(event)
  }

  function setNewName(newName: string) {
    const member = lobby.value?.members.find((m) => m.userId === currentUserId.value)
    if (!member) return
    member.name = newName
  }

  function removeLobbyMemberFromStore(lobbyMemberId: string) {
    if (!lobby.value?.members) return
    lobby.value.members = lobby.value.members.filter((m) => m.id != lobbyMemberId)
  }

  function addMessageToStore(message: Message) {
    const member = lobby.value?.members.find((m) => m.id === message.lobbyMemberId)
    if (!member) return
    member.messages.push(message)
  }

  function sendSystemMessage(message: string) {
    systemMessages.value.push(new SystemMessageViewModel(message))
  }

  function sendDebugMessage(message: string, level: number) {
    if (debugLevel.value !== 0 && level >= debugLevel.value) systemMessages.value.push(new SystemMessageViewModel(message))
  }

  function setDebugging(level: number = 1) {
    debugLevel.value = level
  }

  //#endregion

  //#region actions
  async function getLobby(joinCode: string) {
    const lobby = await LobbyService.getLobbyByCode(joinCode)
    lobby.members.sort((a, b) => compareAsc(a.joined, b.joined))
    setLobby(lobby)
  }

  async function getLobbyEvents(lobbyId: string) {
    const events = await LobbyService.getLobbyEventsById(lobbyId)
    setEvents(events)
  }

  /**
   * Makes each pick and adds the ones the server accepts. Nothing is shown before the server answers (the UI shows
   * them as pending), so a refused pick never appears. Picks are independent: one can fail while others succeed.
   * Returns each request's outcome, with the server's reason for any refusal.
   */
  async function makePicks(pickRequests: PickRequest[]): Promise<{ request: PickRequest; error?: string }[]> {
    const joinCode = lobby.value?.joinCode
    if (!joinCode) return pickRequests.map((request) => ({ request, error: 'Not in a lobby.' }))

    return Promise.all(
      pickRequests.map(async (request) => {
        try {
          addPick(await LobbyService.makePick(joinCode, request))
          return { request }
        } catch (error) {
          return { request, error: getErrorMessage(error, "Couldn't make that pick. Please try again.") }
        }
      })
    )
  }

  async function assignDrink(drinkId: string, recipientId: string) {
    if (!lobby.value) return
    const drink = await LobbyService.assignDrink(lobby.value.joinCode, drinkId, recipientId)
    setDrinkRecipient(drink)
  }

  // Lobby changes below are applied once the server has made them, and throw its refusal (e.g. a name already taken)
  // so the caller can show it. Only chat is optimistic: messages appear instantly and are taken back if sending fails.

  async function changeName(newName: string) {
    if (!lobby.value) return

    await LobbyService.changeName(lobby.value.joinCode, newName)
    setNewName(newName)
  }

  async function removeLobbyMember(lobbyMemberId: string) {
    if (!lobby.value) return

    await LobbyService.removeLobbyMember(lobby.value.joinCode, lobbyMemberId)
    removeLobbyMemberFromStore(lobbyMemberId)
  }

  /** Removes a pick once the server has. Throws the server's refusal, so the caller can show it. */
  async function removePick(pickId: string) {
    if (!lobby.value) return

    await LobbyService.removePick(lobby.value.joinCode, pickId)
    removePickFromStore(pickId)
  }

  /** Adds a bot, then refetches the lobby so the bot appears as the server created it (with its ID). */
  async function addBot(bot: Bot) {
    if (!lobby.value) return

    const joinCode = lobby.value.joinCode
    await LobbyService.addBot(joinCode, bot)
    await getLobby(joinCode)
  }

  /** Shows the message immediately, and takes it back (and rethrows) if the server doesn't accept it. */
  async function sendMessage(messageText: string) {
    if (!lobby.value) return

    const member = lobby.value.members.find((m) => m.userId === currentUserId.value)
    if (!member) return
    const message = { lobbyMemberId: member.id, message: messageText, sent: new Date() } as Message

    addMessageToStore(message)
    try {
      await LobbyService.sendMessage(lobby.value.joinCode, message)
    } catch (error) {
      // The stored copy is a reactive proxy of `message`, so compare the raw objects.
      member.messages = member.messages.filter((m) => toRaw(m) !== message)
      throw error
    }
  }
  //#endregion

  //#region getters
  const isLobbyAdmin = computed(() => currentUserId.value === lobby.value?.createdBy)
  const currentMember = computed(() => lobby.value?.members.find((m) => m.userId === currentUserId.value))

  /** Every member's messages plus local system messages (e.g. command output), oldest first. */
  const chatMessages = computed(() => {
    const memberMessages = (lobby.value?.members ?? []).flatMap(
      (member) => member.messages?.map((msg) => new MessageViewModel(member, msg.message, msg.sent, msg.id)) ?? []
    )
    return [...memberMessages, ...systemMessages.value].sort((a, b) => compareAsc(a.sent, b.sent))
  })
  //#endregion

  return {
    lobby,
    events,
    currentUserId,
    systemMessages,
    debugLevel,
    setLobby,
    setEvents,
    addPick,
    removePickFromStore,
    setDrinkRecipient,
    addLobbyEvent,
    setNewName,
    removeLobbyMemberFromStore,
    addMessageToStore,
    getLobby,
    getLobbyEvents,
    makePicks,
    assignDrink,
    changeName,
    removeLobbyMember,
    removePick,
    addBot,
    sendMessage,
    isLobbyAdmin,
    currentMember,
    chatMessages,
    sendDebugMessage,
    sendSystemMessage,
    setDebugging
  }
})
