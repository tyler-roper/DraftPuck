import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
import { drinkRecipients, drinksToGive, isGiveBarCollapsed } from '@/helpers/giveDrinkHelpers'

const DISMISSED_KEY_PREFIX = 'draftpuck.giveBarDismissed.'

// Shared by every caller on the page: the bar and your own card's "To Give" count open the same sheet, and the bar
// remembers its dismissal per lobby.
const isSheetOpen = ref(false)
const dismissedByLobby = ref<Record<string, string[]>>({})

function readDismissed(joinCode: string): string[] {
  try {
    return JSON.parse(localStorage.getItem(DISMISSED_KEY_PREFIX + joinCode) ?? '[]')
  } catch {
    return []
  }
}

function writeDismissed(joinCode: string, drinkIds: string[]) {
  try {
    localStorage.setItem(DISMISSED_KEY_PREFIX + joinCode, JSON.stringify(drinkIds))
  } catch {
    // Storage blocked (private mode): the dismissal lasts until the page reloads.
  }
}

/**
 * Giving out the drinks your picks earned: the bar over the bottom of the lobby (full, or shrunk to a chip once
 * dismissed), the sheet of people to give one to, and handing one out.
 */
export function useGiveDrinks() {
  const store = useLobbyStore()
  const { lobby, currentMember } = storeToRefs(store)

  const pendingDrinks = computed(() => drinksToGive(currentMember.value))
  const recipients = computed(() => (lobby.value && currentMember.value ? drinkRecipients(lobby.value, currentMember.value.id) : []))

  const dismissedDrinkIds = computed(() => {
    const joinCode = lobby.value?.joinCode
    if (!joinCode) return []
    return dismissedByLobby.value[joinCode] ?? readDismissed(joinCode)
  })
  const isCollapsed = computed(() =>
    isGiveBarCollapsed(
      pendingDrinks.value.map((d) => d.id),
      dismissedDrinkIds.value
    )
  )

  /** Shrinks the bar to its chip until another drink comes in. */
  function collapse() {
    const joinCode = lobby.value?.joinCode
    if (!joinCode) return
    const drinkIds = pendingDrinks.value.map((d) => d.id)
    dismissedByLobby.value = { ...dismissedByLobby.value, [joinCode]: drinkIds }
    writeDismissed(joinCode, drinkIds)
  }

  function openSheet() {
    if (pendingDrinks.value.length > 0) isSheetOpen.value = true
  }

  function closeSheet() {
    isSheetOpen.value = false
  }

  /** Hands your next drink to the recipient. Throws the server's refusal. Closes the sheet once none are left. */
  async function give(recipient: LobbyMember, message?: string) {
    const drink = pendingDrinks.value[0]
    if (!drink) return
    await store.assignDrink(drink.id, recipient.id, message)
    if (pendingDrinks.value.length === 0) closeSheet()
  }

  return { pendingDrinks, recipients, isCollapsed, isSheetOpen, collapse, openSheet, closeSheet, give }
}
