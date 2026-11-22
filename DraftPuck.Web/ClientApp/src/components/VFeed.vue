<script setup lang="ts">
import { computed, ref } from 'vue'
import VFeedList from '@/components/VFeedList.vue'
import VFeedSettings from '@/components/VFeedSettings.vue'
import VDrinkTimeline from '@/components/VDrinkTimeline.vue'
import { useFeedFilters } from '@/composables/useFeedFilters'
import { filterFeedItems } from '@/helpers/feedFilters'
import type FeedItem from '@/models/feedItem'

type View = 'feed' | 'list' | 'settings'

const props = withDefaults(
  defineProps<{
    items: FeedItem[]
  }>(),
  {
    items: () => []
  }
)

const currentView = ref<View>('feed')
const feedRoot = ref<HTMLDivElement>()
const { filters } = useFeedFilters()
const filteredItems = computed(() => filterFeedItems(props.items, filters.value))

function scrollToTop() {
  feedRoot.value?.scrollTo({ top: 0, behavior: 'smooth' })
}
</script>

<template>
  <div ref="feedRoot" style="overflow-y: scroll; background: #13100e" class="text-stone-800 d-flex flex-column">
    <div class="bg-stone-900 text-stone-0 py-1 px-3 ls-2 shadow d-flex align-items-center" style="z-index: 2; position: sticky; top: 0">
      <div class="fs-5 me-2">🚨</div>
      <div>
        <span class="d-block mb-n2">The</span>
        <span class="fs-6 fw-bold d-block text-uppercase">Feed</span>
      </div>
      <div class="ms-auto d-flex">
        <a
          role="button"
          v-if="currentView !== 'feed'"
          @click="currentView = 'feed'"
          class="fs-6 p-3 text-stone-400 d-block m-n3 fw-bold text-uppercase"
          style="text-decoration: none !important"
          >Back To Feed</a
        >
        <template v-else>
          <a role="button" @click="currentView = 'list'" class="p-3 text-stone-400 d-block my-n3 mx-3" style="text-decoration: none !important"
            ><span class="fw-bold text-uppercase">Timeline</span></a
          >
          <a role="button" @click="currentView = 'settings'" class="p-3 text-stone-400 d-block m-n3" style="text-decoration: none !important">
            <span class="fw-bold text-uppercase">Settings</span>
          </a>
        </template>
      </div>
    </div>

    <div class="d-flex flex-column-reverse flex-grow-1" style="justify-content: flex-end">
      <VFeedSettings v-if="currentView === 'settings'" v-model="filters" class="flex-grow-1" />
      <VDrinkTimeline v-else-if="currentView === 'list'" />
      <VFeedList v-else :items="filteredItems" @focused="scrollToTop" />
    </div>
  </div>
</template>
