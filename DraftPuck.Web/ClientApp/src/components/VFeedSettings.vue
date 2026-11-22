<script setup lang="ts">
import VSwitch from '@/components/VSwitch.vue'
import { FEED_FILTER_GROUPS, isGroupShown, setGroupShown, type FeedFilters } from '@/helpers/feedFilters'

const filters = defineModel<FeedFilters>({ required: true })
</script>

<template>
  <div class="feed-settings">
    <div class="settings-intro">Choose what shows in the feed.</div>
    <div v-for="group in FEED_FILTER_GROUPS" :key="group.title" class="fs-6">
      <div class="fw-bold group-title">
        <span>{{ group.title }}</span>
        <a
          role="button"
          class="text-decoration-none text-primary fs-8 ms-3"
          @click="filters = setGroupShown(filters, group, !isGroupShown(filters, group))"
          >{{ isGroupShown(filters, group) ? 'Hide All' : 'Show All' }}</a
        >
      </div>
      <div class="group">
        <VSwitch
          v-for="filter in group.filters"
          :key="filter.key"
          :id="filter.key"
          :model-value="filters[filter.key]"
          @update:model-value="(on: boolean) => (filters = { ...filters, [filter.key]: on })"
          >{{ filter.label }}</VSwitch
        >
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Dark, each group of switches a card. */
.feed-settings {
  padding: 12px 10px 20px;
  color: #e7e5e4;
  background: #13100e;
}

.settings-intro {
  margin: 2px 4px 12px;
  font-size: 13px;
  color: #a8a29e;
}

.group-title {
  display: flex;
  align-items: baseline;
  margin: 4px 4px 8px;
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: #a8a29e;
}

.group {
  margin-bottom: 16px;
  padding: 6px 14px;
  border-radius: 12px;
  background: #1e1b19;
}

.group :deep(.form-check) {
  margin: 0 !important;
  padding-top: 10px;
  padding-bottom: 10px;
  border-bottom: 1px solid #2b2623;
}

.group :deep(.form-check:last-child) {
  border-bottom: 0;
}

.group :deep(.form-check-label) {
  color: #f5f5f4;
}
</style>
