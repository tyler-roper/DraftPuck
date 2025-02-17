import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type Bot from '@/models/bot'
import LobbyService from '@/services/LobbyService'
import { addMilliseconds, compareAsc, differenceInMilliseconds } from 'date-fns'
import type PickRequest from '@/models/pickRequest'
import SystemMessageViewModel from '@/models/systemMessageViewModel'
import SystemService from '@/services/SystemService'

export const useLobbyStore = defineStore('lobby', () => {
  //#region state
  const lobby = ref<Lobby>()
  const events = ref<LobbyEvent[]>([])
  const currentUserId = ref<string>()
  const systemMessages = ref<SystemMessageViewModel[]>([])
  const debugLevel = ref(0)

  const appIsTestMode = ref(false)
  const appStartupTime = ref(new Date())
  const appTestModeStartTime = ref(new Date())
  const currentSystemTime = ref(new Date())
  //#endregion

  //#region mutations
  const setAppSettings = (appSettings: { appIsTestMode: boolean, appStartupTime: Date, appTestModeStartTime: Date }) => {
    appIsTestMode.value = appSettings.appIsTestMode
    appStartupTime.value = appSettings.appStartupTime
    appTestModeStartTime.value = appSettings.appTestModeStartTime
  }
  const setLobby = (_lobby: Lobby) => (lobby.value = _lobby)
  const setEvents = (_events: LobbyEvent[]) => (events.value = _events)
  const setCurrentUserId = (_currentUserId: string) => (currentUserId.value = _currentUserId)

  function addPick(pick: LobbyMemberPick) {
    lobby.value?.members.find((m) => pick.lobbyMemberId === m.id)?.picks.push(pick)
  }

  function updatePick(pick: LobbyMemberPick) {
    const member = lobby.value?.members.find((m) => pick.lobbyMemberId === m.id)
    if (!member) return

    const idx = member.picks.findIndex((p) => p.playerId === pick.playerId && pick.gameId === p.gameId)
    if (idx === -1) return

    member.picks[idx] = pick
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

  function addLobbyMember(lobbyMember: LobbyMember) {
    lobby.value?.members.push(lobbyMember)
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

  function updateSystemTime() {
    const millisecondsSinceStartup = differenceInMilliseconds(new Date(), appStartupTime.value);
    currentSystemTime.value = addMilliseconds(appTestModeStartTime.value, millisecondsSinceStartup)
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

  async function pickPlayer(pickRequest: PickRequest) {
    if (!lobby.value) return
    addPick(pickRequest)

    const persistedPick = await LobbyService.makePick(lobby.value.joinCode, pickRequest)
    updatePick(persistedPick)
  }

  async function assignDrink(drinkId: string, recipientId: string) {
    if (!lobby.value) return
    const drink = await LobbyService.assignDrink(lobby.value.joinCode, drinkId, recipientId)
    setDrinkRecipient(drink)
  }

  async function changeName(newName: string) {
    if (!lobby.value) return

    setNewName(newName)
    await LobbyService.changeName(lobby.value.joinCode, newName)
  }

  async function removeLobbyMember(lobbyMemberId: string) {
    if (!lobby.value) return

    removeLobbyMemberFromStore(lobbyMemberId)
    await LobbyService.removeLobbyMember(lobby.value.joinCode, lobbyMemberId)
  }

  async function removePick(pickId: string) {
    if (!lobby.value) return

    removePickFromStore(pickId)
    await LobbyService.removePick(lobby.value.joinCode, pickId)
  }

  async function addBot(bot: Bot) {
    if (!lobby.value) return

    addLobbyMember(bot)
    await LobbyService.addBot(lobby.value.joinCode, bot)
  }

  async function sendMessage(messageText: string) {
    if (!lobby.value) return

    const member = lobby.value?.members.find((m) => m.userId === currentUserId.value)
    if (!member) return
    const message = { lobbyMemberId: member.id, message: messageText, sent: new Date() } as Message

    addMessageToStore(message)
    await LobbyService.sendMessage(lobby.value.joinCode, message)
  }

  async function initAppSettings() {
    const result = await SystemService.getSettings();
    setAppSettings({ appIsTestMode: result.isTestMode, appStartupTime: result.startupTimeUtc, appTestModeStartTime: result.testModeStartDateTimeUtc})
  }
  //#endregion

  //#region getters
  const isLobbyAdmin = computed(() => currentUserId.value === lobby.value?.createdBy)
  //#endregion

  return {
    lobby,
    events,
    currentUserId,
    systemMessages,
    debugLevel,
    setAppSettings,
    setLobby,
    setEvents,
    setCurrentUserId,
    addPick,
    updatePick,
    removePickFromStore,
    setDrinkRecipient,
    addLobbyEvent,
    setNewName,
    removeLobbyMemberFromStore,
    addLobbyMember,
    addMessageToStore,
    getLobby,
    getLobbyEvents,
    pickPlayer,
    assignDrink,
    changeName,
    removeLobbyMember,
    removePick,
    addBot,
    sendMessage,
    isLobbyAdmin,
    initAppSettings,
    sendDebugMessage,
    sendSystemMessage,
    setDebugging,
    currentSystemTime,
    appIsTestMode,
    updateSystemTime
  }
})
