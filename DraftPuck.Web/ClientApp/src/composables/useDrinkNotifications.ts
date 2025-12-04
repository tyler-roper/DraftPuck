import { ref, nextTick } from 'vue'

const DRINK_ANIMATION_DURATION_MS = 5000

export function useDrinkNotifications() {
  const pendingDrinksForCurrentUser = ref<LobbyEvent[]>([])
  const currentDrink = ref<LobbyEvent>()

  function addDrinkForCurrentUser(lobbyEvent: LobbyEvent) {
    pendingDrinksForCurrentUser.value.push(lobbyEvent)
    if (pendingDrinksForCurrentUser.value.length === 1) {
      processNextDrinkForCurrentUser()
    }
  }

  function processNextDrinkForCurrentUser() {
    if (pendingDrinksForCurrentUser.value.length === 0) return
    currentDrink.value = pendingDrinksForCurrentUser.value[0]

    window.setTimeout(async () => {
      pendingDrinksForCurrentUser.value.splice(0, 1)
      currentDrink.value = undefined
      await nextTick()
      processNextDrinkForCurrentUser()
    }, DRINK_ANIMATION_DURATION_MS)
  }

  return {
    pendingDrinksForCurrentUser,
    currentDrink,
    addDrinkForCurrentUser
  }
}
