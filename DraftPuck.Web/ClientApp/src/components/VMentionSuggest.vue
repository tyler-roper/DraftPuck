<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { mentionMatchIndex } from '@/helpers/chatHelpers'

/** The members a mention being typed could be for, above the chat box. Picking one is the parent's to apply. */
const props = defineProps<{ members: LobbyMember[]; query: string; activeIndex: number }>()
const emit = defineEmits<{ pick: [member: LobbyMember]; hover: [index: number] }>()

const list = ref<HTMLUListElement>()

// Keep the highlighted row in view as the arrow keys move it.
watch(
  () => props.activeIndex,
  async (index) => {
    await nextTick()
    list.value?.children[index]?.scrollIntoView({ block: 'nearest' })
  }
)

/** The name split around the part that matches what's been typed, for highlighting. */
function nameParts(name: string) {
  const at = Math.max(0, mentionMatchIndex(name, props.query))
  return { before: name.slice(0, at), match: name.slice(at, at + props.query.length), after: name.slice(at + props.query.length) }
}

function avatarStyle(member: LobbyMember) {
  return member.avatarPath && !member.isGuest ? { backgroundImage: `url(${member.avatarPath})` } : {}
}
</script>

<template>
  <!-- mousedown is prevented so tapping a name doesn't take focus from the chat box (and close the keyboard). -->
  <ul ref="list" class="mention-suggest" role="listbox" aria-label="Mention someone" @mousedown.prevent>
    <li v-for="(member, index) in members" :key="member.id" role="option" :aria-selected="index === activeIndex"
      class="mention-option" :class="{ active: index === activeIndex }" @click="emit('pick', member)" @mousemove="emit('hover', index)">
      <span class="avatar" :style="avatarStyle(member)">
        <template v-if="!member.avatarPath || member.isGuest">{{ member.name.charAt(0).toUpperCase() }}</template>
      </span>
      <span class="min-w-0">
        <span class="d-block name text-truncate">
          @{{ nameParts(member.name).before }}<mark>{{ nameParts(member.name).match }}</mark>{{ nameParts(member.name).after }}
        </span>
        <span v-if="member.title?.text || member.isGuest" class="d-block title text-truncate">
          {{ member.isGuest ? 'Guest' : member.title?.text }}
        </span>
      </span>
    </li>
  </ul>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Sized to its matches: a row each, up to five, then it scrolls.
.mention-suggest {
  position: absolute;
  z-index: 20;
  left: 10px;
  right: 10px;
  bottom: calc(100% - 6px);
  max-height: calc(5 * 52px + 12px);
  margin: 0;
  padding: 6px;
  list-style: none;
  overflow-y: auto;
  overscroll-behavior: contain;
  background: map-get($custom-colors, 'stone-900');
  border: 1px solid map-get($custom-colors, 'stone-700');
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.45);
}

.mention-option {
  display: flex;
  align-items: center;
  gap: 10px;
  height: 52px;
  padding: 0 10px;
  border-radius: 8px;
  cursor: pointer;
  color: map-get($custom-colors, 'stone-0');
}

.mention-option.active {
  background: map-get($custom-colors, 'stone-700');
}

.avatar {
  flex: none;
  display: grid;
  place-items: center;
  width: 34px;
  height: 34px;
  border-radius: 50%;
  font-weight: 800;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;
}

.min-w-0 {
  min-width: 0;
}

.name {
  font-weight: 700;
  line-height: 1.2;
}

.name mark {
  padding: 0;
  color: map-get($custom-colors, 'amber-500');
  background: none;
}

.title {
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: map-get($custom-colors, 'stone-400');
}
</style>
