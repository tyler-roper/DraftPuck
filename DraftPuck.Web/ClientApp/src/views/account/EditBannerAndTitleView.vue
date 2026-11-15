<script setup lang="ts">
import { useUserStore } from '@/stores/user'
import { ref, computed, watch } from 'vue'
import VUser from '@/components/VUser.vue'
import VIcon from '@/components/VIcon.vue'
import VFlipCard from '@/components/VFlipCard.vue'
import { useSingleFlip } from '@/composables/useSingleFlip'
import { howEarned } from '@/helpers/earnedHelpers'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import { storeToRefs } from 'pinia'
import { useToast } from '@/composables/useToast'
import { useRouter } from 'vue-router'

type View = 'banners' | 'titles'

//#region data
const userStore = useUserStore()
const { currentUser, banners: allBanners, titles: allTitles } = storeToRefs(userStore)
const userPreview = ref<User>(JSON.parse(JSON.stringify(currentUser.value)))
const currentView = ref<View>('banners')
const isSaving = ref(false)
const toast = useToast()
const router = useRouter()
// One card at a time can be flipped to show how it was earned.
const flip = useSingleFlip()
const achievements = computed(() => currentUser.value?.achievements ?? [])
watch(currentView, () => flip.reset())

const genericBanners = computed(() => allBanners.value.filter((b) => !b.achievementId))
const genericTitles = computed(() => allTitles.value.filter((t) => !t.achievementId))
const isDirty = computed(
  () => userPreview.value.banner.id !== currentUser.value?.banner.id || userPreview.value.title.id !== currentUser.value?.title.id
)

const availableBanners = computed(() => {
  if (!currentUser.value) return []

  const merged = [...currentUser.value.ownedBanners, ...genericBanners.value]

  // Deduplicate by ID
  const byId = new Map()
  merged.forEach((b) => byId.set(b.id, b))

  return Array.from(byId.values())
})

const availableTitles = computed(() => {
  if (!currentUser.value) return []

  const merged = [...currentUser.value.ownedTitles, ...genericTitles.value]

  const byId = new Map()
  merged.forEach((t) => byId.set(t.id, t))

  return Array.from(byId.values()).sort((a, b) => (a.text || '').localeCompare(b.text || ''))
})

//#endregion

//#region methods
function selectBanner(banner: Banner) {
  userPreview.value.banner = banner
  flip.reset()
}

function selectTitle(title: Title) {
  userPreview.value.title = title
  flip.reset()
}

function resetUserPreview() {
  userPreview.value = JSON.parse(JSON.stringify(currentUser.value))
}

function discard() {
  resetUserPreview()
}

async function save() {
  try {
    isSaving.value = true
    await userStore.updateUser({ bannerId: userPreview.value.banner.id, titleId: userPreview.value.title.id })
    toast.success('Profile saved!', { timeout: 2000 })
    router.replace(`/u/${currentUser.value!.nickname}`)
  } catch {
    console.error('Unable to update user.')
  } finally {
    isSaving.value = false
  }
}
//#endregion
</script>

<template>
  <HeaderLayout title="Banner & Title" :is-dirty="isDirty" @discard="discard" @save="save" :is-saving="isSaving">
    <template #header>
      <div class="bg-stone-800 d-flex fs-6 text-uppercase fw-bold">
        <a
          role="button"
          @click="currentView = 'banners'"
          class="view-tab d-flex align-items-center justify-content-center flex-grow-1 p-3"
          :class="{ 'is-selected': currentView === 'banners' }"
        >
          <VIcon :prefix="currentView === 'banners' ? 'sr' : 'rr'" icon="bookmark" class="me-2" />
          <span>Banners</span>
        </a>
        <a
          role="button"
          @click="currentView = 'titles'"
          class="view-tab flex-grow-1 text-center p-3"
          :class="{ 'is-selected': currentView === 'titles' }"
        >
          <VIcon :prefix="currentView === 'titles' ? 'sr' : 'rr'" icon="comment-quote" class="me-2" />
          <span>Titles</span>
        </a>
      </div>
    </template>

    <div v-if="currentView === 'banners'" class="p-3 my-n3">
      <VFlipCard
        v-for="banner in availableBanners"
        :key="banner.id"
        :selected="banner.id === userPreview?.banner.id"
        :flipped="flip.isFlipped(banner.id)"
        :how-earned="howEarned(banner, achievements)"
        @select="selectBanner(banner)"
        @flip="flip.toggle(banner.id)"
      >
        <div class="banner rounded-md" :style="{ 'background-image': `url(${banner.imagePath})` }"></div>
      </VFlipCard>
      <a role="button" class="banner-container rounded-lg my-3" v-for="n in allBanners.length - availableBanners.length" :key="n">
        <div class="banner locked rounded-md bg-gradient">
          <div class="lock">
            <VIcon class="fs-2" prefix="sr" icon="lock" />
          </div>
        </div>
      </a>
    </div>

    <div v-if="currentView === 'titles'" class="p-3 my-n3">
      <VFlipCard
        v-for="title in availableTitles"
        :key="title.id"
        compact
        :selected="title.id === userPreview?.title.id"
        :flipped="flip.isFlipped(title.id)"
        :how-earned="howEarned(title, achievements)"
        @select="selectTitle(title)"
        @flip="flip.toggle(title.id)"
      >
        <div class="banner title rounded-md">{{ title.text }}</div>
      </VFlipCard>
      <a role="button" class="banner-container rounded-lg my-3 p-0" v-for="n in allTitles.length - availableTitles.length" :key="n">
        <div class="banner title locked rounded-md bg-gradient">
          <div class="lock">
            <VIcon class="fs-2" prefix="sr" icon="lock" />
          </div>
        </div>
      </a>
    </div>

    <template #footer>
      <VUser :user="userPreview" />
    </template>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.overflow-hidden {
  overflow: hidden;
}

a.view-tab {
  color: map-get($custom-colors, 'stone-400');
  border-bottom: 5px solid transparent;
}

a.view-tab.is-selected {
  color: map-get($custom-colors, 'stone-0');
  border-bottom: 5px solid map-get($custom-colors, 'primary');
  background-color: rgba(map-get($custom-colors, 'stone-0'), 0.05);
}

.banner {
  width: 100%;
  height: 70px;
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center;
  display: flex;
  align-items: center;
  justify-content: center;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: map-get($custom-colors, 'stone-0');
}

.banner.title {
  padding: 10px;
  font-size: 16px;
  height: auto;
}

// Locked placeholders; owned banners and titles are framed by VFlipCard.
.banner-container {
  display: block;
  border: 1px solid map-get($custom-colors, 'stone-400');
  padding: 8px;
}

.banner.locked .lock {
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 15px rgba(255, 255, 255, 0.3);
  width: 50px;
  height: 50px;
  background-color: map-get($custom-colors, 'stone-900');
  border-radius: 50%;
  color: map-get($custom-colors, 'stone-0');
}

.banner.title.locked .lock {
  width: 40px;
  height: 40px;
}
</style>
