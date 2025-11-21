import { createRouter, createWebHistory, RouterView } from 'vue-router'
import { setPageMetaTags, setPageTitle } from '@/helpers/routerHelpers'
import { useUserStore } from '@/stores/user'

const requiresLogin = true

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'Home',
      component: () => import('@/views/HomeView.vue'),
      meta: {
        title: 'DRAFTPUCK - A live hockey drinking game.',
        metaTags: [
          { name: 'title', content: 'DRAFTPUCK - A live hockey drinking game.' },
          {
            name: 'description',
            content: 'Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!'
          },

          { property: 'og:type', content: 'website' },
          { property: 'og:url', content: 'https://draftpuck.com/' },
          { property: 'og:title', content: 'DRAFTPUCK - A live hockey drinking game.' },
          {
            property: 'og:description',
            content: 'Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!'
          },
          { property: 'og:image', content: '/dist/assets/meta-img.png' },

          { property: 'twitter:card', content: 'summary_large_image' },
          { property: 'twitter:url', content: 'https://draftpuck.com/' },
          { property: 'twitter:title', content: 'DRAFTPUCK - A live hockey drinking game.' },
          {
            property: 'twitter:description',
            content: 'Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!'
          },
          { property: 'twitter:image', content: '/dist/assets/meta-img.png' }
        ]
      }
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/LoginView.vue'),
      meta: { redirectIfLoggedIn: true }
    },
    {
      path: '/join',
      name: 'Join',
      component: () => import('@/views/JoinView.vue'),
      meta: { redirectIfLoggedIn: true }
    },
    {
      path: '/lobby/:joinCode',
      name: 'Lobby',
      props: true,
      component: () => import('@/views/LobbyView.vue'),
      meta: {
        title: `DRAFTPUCK (Lobby: {{joinCode}})`,
        metaTags: [
          { name: 'title', content: `DRAFTPUCK (Lobby: {{joinCode}})` },
          { property: 'og:title', content: `DRAFTPUCK (Lobby: {{joinCode}})` },
          { property: 'twitter:title', content: `DRAFTPUCK (Lobby: {{joinCode}})` }
        ]
      }
    },
    {
      path: '/account',
      name: 'Account',
      component: RouterView,
      children: [
        { name: 'AccountRedirect', path: '', redirect: { name: 'AccountSettings' } },
        {
          path: 'banner',
          name: 'Banner',
          component: () => import('@/views/EditBannerAndTitleView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'lobbies',
          name: 'Lobbies',
          component: () => import('@/views/LobbyListView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'settings',
          name: 'Settings',
          component: () => import('@/views/AccountSettingsView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'avatar',
          name: 'Avatar',
          component: () => import('@/views/EditAvatarView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'discord',
          name: 'Discord',
          component: () => import('@/views/DiscordView.vue'),
          meta: { requiresLogin }
        }
      ]
    },
    {
      path: '/u/:username',
      name: 'Profile',
      props: true,
      component: () => import('@/views/ProfileView.vue')
    },
    {
      path: '/u/:username/achievements',
      name: 'Achievements',
      props: true,
      component: () => import('@/views/AchievementsView.vue')
    }
  ]
})

router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()
  let initializePromise

  if (!userStore.isReady && (to.meta.requiresLogin || to.meta.redirectIfLoggedIn)) initializePromise = userStore.initialize()

  if (userStore.isLoggedIn && to.meta?.redirectIfLoggedIn === true) {
    next({ name: 'Home' })
    return
  }

  if (to.meta.requiresLogin === true) {
    await initializePromise
    if (!userStore.isLoggedIn) {
      next({ name: 'Login', query: { redirect: to.path } })
      return
    }
  }

  setPageMetaTags((to.meta?.metaTags as MetaTag[]) ?? [], to.params)
  setPageTitle(to.meta?.title as string, to.params)

  next()
})

export default router
