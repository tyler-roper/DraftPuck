import { createRouter, createWebHistory, RouterView } from 'vue-router'
import { setPageMetaTags, setPageTitle } from '@/helpers/routerHelpers'
import { useUserStore } from '@/stores/user'

const requiresLogin = true
const requiresAdmin = true

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
      name: 'LobbyRoot',
      props: true,
      component: RouterView,
      children: [
        {
          path: '',
          name: 'Lobby',
          props: true,
          component: () => import('@/views/lobby/LobbyView.vue'),
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
          path: 'review',
          name: 'LobbyReview',
          props: true,
          component: () => import('@/views/lobby/LobbyReviewView.vue')
        }
      ]
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
          component: () => import('@/views/account/EditBannerAndTitleView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'lobbies',
          name: 'Lobbies',
          component: () => import('@/views/account/LobbyListView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'settings',
          name: 'Settings',
          component: () => import('@/views/account/AccountSettingsView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'avatar',
          name: 'Avatar',
          component: () => import('@/views/account/EditAvatarView.vue'),
          meta: { requiresLogin }
        },
        {
          path: 'discord',
          name: 'Discord',
          component: () => import('@/views/account/DiscordView.vue'),
          meta: { requiresLogin }
        }
      ]
    },
    {
      path: '/u/:username',
      name: 'ProfileRoot',
      props: true,
      component: RouterView,
      children: [
        {
          path: '',
          name: 'Profile',
          props: true,
          component: () => import('@/views/profile/ProfileView.vue')
        },
        {
          path: 'achievements',
          name: 'Achievements',
          props: true,
          component: () => import('@/views/profile/AchievementsView.vue')
        }
      ]
    },
    {
      path: '/admin',
      name: 'AdminRoot',
      component: RouterView,
      meta: { requiresAdmin },
      children: [
        {
          path: '',
          name: 'Admin',
          component: () => import('@/views/admin/AdminView.vue')
        },
        {
          path: 'users',
          name: 'AdminUsers',
          component: () => import('@/views/admin/UsersView.vue')
        },
        {
          path: 'lobbies',
          name: 'AdminLobbies',
          component: () => import('@/views/admin/LobbiesView.vue')
        }
      ]
    }
  ]
})

router.beforeEach(async (to, _from, next) => {
  const userStore = useUserStore()
  const requiresLogin = to.matched.some((r) => r.meta.requiresLogin || r.meta.requiresAdmin)
  const requiresAdmin = to.matched.some((r) => r.meta.requiresAdmin)
  const redirectIfLoggedIn = to.matched.some((r) => r.meta.redirectIfLoggedIn)

  let initializePromise

  if (!userStore.isReady && (requiresLogin || redirectIfLoggedIn)) {
    initializePromise = userStore.initialize()
  }

  if (redirectIfLoggedIn && userStore.isLoggedIn) {
    return next({ name: 'Home' })
  }

  if (requiresLogin) {
    if (initializePromise) await initializePromise

    if (!userStore.isLoggedIn) {
      return next({ name: 'Login', query: { redirect: to.path } })
    }
  }

  if (requiresAdmin && !userStore.isAdmin) {
    return next({ name: 'Home' })
  }

  setPageMetaTags((to.meta?.metaTags as MetaTag[]) ?? [], to.params)
  setPageTitle(to.meta?.title as string, to.params)
  next()
})

export default router
