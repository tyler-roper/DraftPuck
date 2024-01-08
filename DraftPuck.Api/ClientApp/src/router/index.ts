import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import { setPageMetaTags, setPageTitle } from '@/helpers/routerHelpers'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'Home',
      component: HomeView,
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
    }
  ]
})

router.beforeEach((to, _from, next) => {
  setPageMetaTags((to.meta?.metaTags as MetaTag[]) ?? [], to.params)
  setPageTitle(to.meta?.title as string, to.params)
  next()
})

export default router
