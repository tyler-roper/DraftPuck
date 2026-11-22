<script setup lang="ts">
import VFeedItem from '@/components/VFeedItem.vue'
import { useNewArrivals } from '@/composables/useNewArrivals'
import { useFeedFocus } from '@/composables/useFeedFocus'
import { goalKeyFor } from '@/helpers/feedHelpers'
import type FeedItem from '@/models/feedItem'

const props = defineProps<{
  items: FeedItem[]
}>()

const emit = defineEmits<{
  // Focused on a goal: the page scrolls to the top, where the goal and its drinks now are.
  focused: []
}>()

const { focus, goalsByKey, follow, clear, isCollapsed } = useFeedFocus(() => props.items)

// Only items that just arrived slide in; not the first render, a load, or items a filter brings back.
const arrivals = useNewArrivals(() => props.items.map((item) => item.key))

function followLink(goalKey: string) {
  if (follow(goalKey)) emit('focused')
}
</script>

<template>
  <!-- Keyed by item, not index, so an item inserted mid-list doesn't shift every key after it. -->
  <TransitionGroup name="feed-slide" tag="div" class="feed-list">
    <VFeedItem
      v-for="item in items"
      :key="item.key"
      :item="item"
      :linked-goal="goalsByKey.get(goalKeyFor(item) ?? '')"
      :class="{ 'is-new': arrivals.isNew(item.key), collapsed: isCollapsed(item.key) }"
      @follow-link="followLink"
      :style="{ '--arrival': Math.max(arrivals.arrivalOrder(item.key), 0) }"
    />
  </TransitionGroup>
  <!-- After the list in the markup: the parent is a reversed column, so this shows above it. -->
  <div v-if="focus" class="focus-bar">
    <span
      >Showing <strong>{{ focus.label }}</strong> and its drinks</span
    >
    <button type="button" @click="clear">✕ Show all</button>
  </div>
</template>

<style scoped>
/* New arrivals slide down into place at the top (newest first, 80ms apart), and the items below glide down to make
   room. Nothing animates when items leave. */
.feed-slide-enter-active.is-new {
  transition:
    opacity 0.35s ease,
    transform 0.35s ease;
  transition-delay: calc(var(--arrival) * 80ms);
}

.feed-slide-enter-from.is-new {
  opacity: 0;
  transform: translateY(-100%);
}

.feed-slide-move {
  transition: transform 0.35s ease;
}

/* The items, newest at the top (the column is reversed), as cards on a dark background. */
.feed-list {
  flex-grow: 1;
  display: flex;
  flex-direction: column-reverse;
  justify-content: flex-end;
  padding: 6px 10px;
  background: #13100e;
}

/* Spacing as margins (not gap), so a collapsed item leaves no space behind. */
.feed-list > * {
  margin: 4px 0;
  max-height: 600px;
  transition:
    opacity 0.3s ease,
    max-height 0.35s ease,
    margin 0.35s ease,
    min-height 0.35s ease;
}

/* Focused on a goal: everything else fades and collapses, so the goal and its drinks sit together. */
.feed-list > .collapsed {
  opacity: 0;
  max-height: 0;
  min-height: 0;
  margin: 0;
  border-width: 0;
  pointer-events: none;
}

.focus-bar {
  position: sticky;
  top: 52px;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin: 8px 10px 0;
  padding: 8px 12px;
  border-radius: 999px;
  font-size: 13px;
  color: #fde68a;
  background: #2b2623;
  border: 1px solid #ffb100;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.5);
}

.focus-bar strong {
  color: #fff;
}

.focus-bar button {
  border: 0;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 800;
  color: #1c1917;
  background: #ffb100;
}

@media (prefers-reduced-motion: reduce) {
  .feed-slide-enter-active.is-new,
  .feed-slide-move {
    transition: none;
  }
}
</style>
