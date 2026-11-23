<script setup lang="ts">
export type LobbyView = 'feed' | 'game' | 'lobby' | 'chat' | 'picks'

/** The lobby page's tabs on a phone, with a badge for unread chat. Drinks to give have their own bar (VGiveDrinkBar). */
defineProps<{
  view: LobbyView
  unreadCount: number
  unreadMentionCount: number
}>()

const emit = defineEmits<{ select: [view: LobbyView] }>()
</script>

<template>
  <div class="bottom-nav d-flex d-sm-none bg-stone-900 shadow fw-bold">
    <a role="button" class="text-center p-2 text-white" :class="{ active: view === 'lobby' }" @click="emit('select', 'lobby')">
      <i class="fi fi-rr-users-alt"></i>
      <br />
      <span>LOBBY</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: view === 'feed' }" @click="emit('select', 'feed')">
      <i class="fi fi-rr-list"></i><br />
      <span>FEED</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: view === 'picks' }" @click="emit('select', 'picks')">
      <i class="fi fi-rs-hockey-mask"></i><br />
      <span>PICKS</span>
    </a>
    <a role="button" class="text-center p-2 text-white d-none" :class="{ active: view === 'game' }" @click="emit('select', 'game')">
      <i class="fi fi-rr-hockey-puck"></i><br />
      <span>SCORES</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: view === 'chat' }" @click="emit('select', 'chat')">
      <span v-if="unreadCount > 0" class="drink-badge" :class="{ 'bg-primary': unreadMentionCount > 0 }">
        <span>{{ unreadMentionCount > 0 ? '📢' : '💬' }}</span>
        <span>{{ unreadCount }}</span>
      </span>
      <i v-else class="fi fi-rr-comment-alt"></i>
      <br />
      <span>CHAT</span>
    </a>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.bottom-nav {
  border: 2px solid map-get($custom-colors, 'stone-900');
  box-shadow: 0 0 10px black;
  position: relative;
  z-index: 10;
}

.bottom-nav > a {
  display: block;
  width: calc(100% / 3);
  text-decoration: none !important;
  position: relative;
}

.bottom-nav > a:not(.active):hover {
  background-color: map-get($custom-colors, 'stone-800') !important;
}

.bottom-nav > a.active {
  background-color: map-get($custom-colors, 'stone-300') !important;
  color: map-get($custom-colors, 'stone-900') !important;
}

.bottom-nav > a:not(:first-child) {
  border-left: 1px solid map-get($custom-colors, 'stone-800');
}

.drink-badge {
  display: inline-block;
  background-color: map-get($custom-colors, 'stone-0');
  color: map-get($custom-colors, 'stone-900');
  padding-left: 7px;
  padding-right: 9px;
  border-radius: 20px;
}
</style>
