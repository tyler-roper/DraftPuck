import { ref } from 'vue'
import { defineStore } from 'pinia'
import UserService from '@/services/UserService'

export const useUserStore = defineStore('user', () => {
  //#region state
  const currentUser = ref<User>()
  //#endregion

  //#region mutations
  const setCurrentUser = (user: User) => (currentUser.value = user)
  //#endregion

  //#region actions
  async function initUser() {
    const userId = localStorage.getItem('userId')
    let isValidUser = false

    if (userId) {
      try {
      await getUserById(userId)
      isValidUser = true
      } catch {}
    }

    if (!isValidUser) {
      await createUser()
    }
    return currentUser.value
  }

  const getUserById = async (id: string) => {
    const user = await UserService.getUserById(id)
    setCurrentUser(user)
  }

  const createUser = async () => {
    const user = await UserService.createUser()
    localStorage.setItem('userId', user.id)
    setCurrentUser(user)
  }

  const saveUserNotificationPreferences = async (preferences: UserNotificationPreferencesRequestModel) => {
    if (!currentUser.value) return
    const user = await UserService.updateNotificationPreferences(currentUser.value.id, preferences)
    setCurrentUser(user)
  }
  //#endregion

  //#region getters
  //#endregion

  return {
    currentUser,
    setCurrentUser,
    initUser,
    saveUserNotificationPreferences
  }
})
