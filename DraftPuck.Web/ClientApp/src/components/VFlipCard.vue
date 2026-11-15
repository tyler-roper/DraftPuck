<script setup lang="ts">
import { format } from 'date-fns'
import VIcon from '@/components/VIcon.vue'
import type { HowEarned } from '@/helpers/earnedHelpers'

/**
 * A banner or title in the picker that can flip over to show how it was earned. The front is the slot. Tapping the card
 * selects it, or turns it back over if it's flipped; the (i) button flips it. Without `howEarned` it doesn't flip.
 */
const props = defineProps<{
  selected: boolean
  flipped: boolean
  howEarned: HowEarned | null
  /** For short cards (titles): one line of description instead of two. */
  compact?: boolean
}>()

const emit = defineEmits<{ select: []; flip: [] }>()

function onCardClick() {
  if (props.flipped) emit('flip')
  else emit('select')
}
</script>

<template>
  <div class="flip-card my-3" :class="{ flipped }">
    <a role="button" class="banner-container rounded-lg" :class="{ selected }" @click="onCardClick">
      <div class="flipper">
        <div class="face front" :aria-hidden="flipped">
          <slot />
        </div>
        <div v-if="howEarned" class="face back rounded-md" :class="{ compact }" :aria-hidden="!flipped">
          <template v-if="howEarned.kind === 'achievement'">
            <div class="name text-truncate">
              {{ howEarned.name }}
              <span v-if="howEarned.dateEarned" class="earned">· Earned {{ format(howEarned.dateEarned, 'MMM d, yyyy') }}</span>
            </div>
            <div class="description">{{ howEarned.description }}</div>
          </template>
          <template v-else>
            <div class="name">Default</div>
            <div class="description">Every player has this one.</div>
          </template>
        </div>
      </div>
    </a>
    <button
      v-if="howEarned"
      type="button"
      class="info"
      :aria-label="flipped ? 'Show the front' : 'How you got this'"
      :aria-pressed="flipped"
      @click="emit('flip')"
    >
      <VIcon :prefix="flipped ? 'sr' : 'rr'" icon="info" />
    </button>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.flip-card {
  position: relative;
}

.banner-container {
  display: block;
  border: 1px solid map-get($custom-colors, 'stone-400');
  padding: 8px;
  perspective: 600px;
}

.banner-container.selected {
  padding: 6px;
  border: 3px solid map-get($custom-colors, 'primary');
  box-shadow: 0 0 20px rgba(map-get($custom-colors, 'primary'), 0.5);
}

// The front sets the card's size; the back sits over it, on the other side.
.flipper {
  position: relative;
  transition: transform 0.45s ease;
  transform-style: preserve-3d;
}

.flipped .flipper {
  transform: rotateX(180deg);
}

.face {
  backface-visibility: hidden;
  -webkit-backface-visibility: hidden;
}

.back {
  position: absolute;
  inset: 0;
  transform: rotateX(180deg);
  display: flex;
  flex-direction: column;
  justify-content: center;
  // Room on the right for the (i) button.
  padding: 4px 36px 4px 12px;
  overflow: hidden;
  background-color: map-get($custom-colors, 'stone-900');
  color: map-get($custom-colors, 'stone-0');
  line-height: 1.25;
}

.back .name {
  font-weight: bold;
  font-size: 14px;
}

.back .earned {
  font-weight: normal;
  font-size: 12px;
  color: map-get($custom-colors, 'stone-400');
}

.back .description {
  font-size: 12px;
  color: map-get($custom-colors, 'stone-300');
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.back.compact .description {
  -webkit-line-clamp: 1;
}

.info {
  position: absolute;
  right: 4px;
  bottom: 4px;
  z-index: 1;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: 50%;
  padding: 0;
  font-size: 14px;
  background-color: rgba(map-get($custom-colors, 'stone-900'), 0.75);
  color: map-get($custom-colors, 'stone-0');
}

@media (prefers-reduced-motion: reduce) {
  .flipper {
    transition: none;
  }
}
</style>
