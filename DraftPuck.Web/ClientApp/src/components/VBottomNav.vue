<script setup lang="ts">
type View = 'feed' | 'game' | 'lobby' | 'chat' | 'picks'

defineProps<{
  currentView: View
  pendingDrinkCount: number
  unseenMessageCount: number
  unseenMentionsCount: number
}>()

const emit = defineEmits<{
  setView: [view: View]
  setViewToChat: []
}>()
</script>

<template>
  <div class="bottom-nav d-flex d-sm-none bg-stone-900 shadow fw-bold">
    <a role="button" class="text-center p-2 text-white" :class="{ active: currentView === 'lobby' }" @click="emit('setView', 'lobby')">
      <i v-if="pendingDrinkCount <= 0" class="fi fi-rr-users-alt"></i>
      <span v-if="pendingDrinkCount > 0" class="drink-badge">🚨 {{ pendingDrinkCount }}</span>
      <br />
      <span>LOBBY</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: currentView === 'feed' }" @click="emit('setView', 'feed')">
      <i class="fi fi-rr-list"></i><br />
      <span>FEED</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: currentView === 'picks' }" @click="emit('setView', 'picks')">
      <i class="fi fi-rs-hockey-mask"></i><br />
      <span>PICKS</span>
    </a>
    <a role="button" class="text-center p-2 text-white d-none" :class="{ active: currentView === 'game' }" @click="emit('setView', 'game')">
      <i class="fi fi-rr-hockey-puck"></i><br />
      <span>SCORES</span>
    </a>
    <a role="button" class="text-center p-2 text-white" :class="{ active: currentView === 'chat' }" @click="emit('setViewToChat')">
      <i v-if="unseenMessageCount <= 0" class="fi fi-rr-comment-alt"></i>
      <span v-if="unseenMessageCount > 0" class="drink-badge" :class="{ 'bg-primary': unseenMentionsCount > 0 }">
        <span v-if="unseenMentionsCount > 0">📢</span>
        <span v-if="unseenMentionsCount <= 0">💬</span>
        <span>{{ unseenMessageCount }}</span>
      </span>
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
