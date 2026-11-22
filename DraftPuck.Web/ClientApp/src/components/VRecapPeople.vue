<script setup lang="ts">
import VRecapAvatar from '@/components/VRecapAvatar.vue'

/** The row of members on the Stats tab: tap one to see their night. */
defineProps<{
  people: LobbyMember[]
}>()

const selectedId = defineModel<string>({ required: true })
</script>

<template>
  <div class="people" role="tablist" aria-label="Whose night">
    <button
      v-for="m in people"
      :key="m.id"
      type="button"
      role="tab"
      class="person"
      :class="{ active: m.id === selectedId }"
      :aria-selected="m.id === selectedId"
      @click="selectedId = m.id"
    >
      <VRecapAvatar :member="m" class="person-avatar" />
      <span class="person-name">{{ m.name }}</span>
    </button>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

$amber: map-get($custom-colors, 'primary');

// Room above and below, so the selected avatar's ring and lift aren't clipped by the scrolling row.
.people {
  display: flex;
  gap: 6px;
  overflow-x: auto;
  padding: 8px 6px 6px;
  margin: -6px -6px 0;
  scrollbar-width: none;
}

.person {
  flex: none;
  width: 64px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 0;
  border: 0;
  background: none;
  color: map-get($custom-colors, 'stone-400');
  opacity: 0.7;
  transition: opacity 0.2s;

  .person-avatar {
    width: 44px;
    height: 44px;
    font-size: 17px;
    transition:
      box-shadow 0.2s,
      transform 0.2s;
  }

  &.active {
    opacity: 1;
    color: white;

    .person-avatar {
      box-shadow:
        0 0 0 2px map-get($custom-colors, 'stone-900'),
        0 0 0 4px $amber,
        0 0 14px rgba($amber, 0.5);
      transform: scale(1.06);
    }
  }
}

.person-name {
  max-width: 100%;
  font-size: 12px;
  font-weight: 700;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (prefers-reduced-motion: reduce) {
  .person .person-avatar {
    transition: none;
  }
}
</style>
