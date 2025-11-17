import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import UserService from '@/services/UserService'
import AuthService from '@/services/AuthService'
import BannerService from '@/services/BannerService'
import TitleService from '@/services/TitleService'
import TokenService from '@/services/TokenService'

export const useUserStore = defineStore('user', () => {
  //#region state
  const currentUser = ref<User>()
  const isReady = ref(false)
  const banners = ref<Array<Banner>>([])
  const titles = ref<Array<Title>>([])
  //#endregion

  //#region getters
  const isLoggedIn = computed(() => isReady.value === true && currentUser.value && currentUser.value.isGuest === false)
  const isAdmin = computed(() => isReady.value === true && currentUser.value?.isAdmin === true)
  //#endregion

  //#region mutations
  const setCurrentUser = (user: User | undefined) => (currentUser.value = user)
  //#endregion

  //#region actions
  let initPromise: Promise<User | undefined> | null = null


  async function initialize() {
    if (isReady.value) return currentUser.value
    if (initPromise) return initPromise

    initPromise = (async () => {
      try {
        await initUser()
        isReady.value = true
        preloadBannersAndTitles()
        return currentUser.value
      } catch (err) {
        console.error('Failed to initialize user:', err)
      } finally {
        isReady.value = true
        initPromise = null
      }
    })()

    return initPromise
  }

  const initUser = async () => {
    let authResponse
    try {
      authResponse = await AuthService.useRefreshToken()
    } catch {
      authResponse = await UserService.createGuest()
    }

    if (authResponse) {
      TokenService.storeTokens(authResponse)
      setCurrentUser(authResponse.user)
    } else {
      throw new Error("Could not set current user.");
    }
  }

  const preloadBannersAndTitles = async () => {
    const results = await Promise.all([BannerService.getAllBanners(), TitleService.getAllTitles()])
    banners.value = results[0]
    titles.value = results[1]
  }

  const login = async (email: string, password: string) => {
    try {
      const authResponse = await AuthService.login(email, password)
      TokenService.storeTokens(authResponse)
      setCurrentUser(authResponse.user)
    } catch {
      //noop
    }
  }

  const logout = async () => {
    await AuthService.logout()
    TokenService.clearTokens()
    await initUser();
  }

  const updateUser = async (request: UpdateUserRequest) => {
    if (!currentUser.value?.id) return
    const user = await UserService.updateUser(currentUser.value.id, request)
    setCurrentUser(user)
  }
  //#endregion

  return {
    banners,
    titles,
    currentUser,
    isLoggedIn,
    isAdmin,
    isReady,
    login,
    logout,
    setCurrentUser,
    initialize,
    updateUser
  }
})
