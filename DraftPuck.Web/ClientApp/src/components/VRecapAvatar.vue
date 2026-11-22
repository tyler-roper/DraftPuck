<script setup lang="ts">
import { computed } from 'vue'
import { memberAvatar } from '@/helpers/recapHelpers'

/** A member's round avatar on the post-game page: their picture or initial. Size it from the parent (width, height, font-size). */
const props = defineProps<{
  member: Pick<LobbyMember, 'name' | 'avatarPath' | 'isGuest'>
}>()

const avatar = computed(() => memberAvatar(props.member))
</script>

<template>
  <span class="avatar" :style="avatar.style">{{ avatar.initial }}</span>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.avatar {
  flex: none;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 800;
  line-height: 1;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;
}
</style>
