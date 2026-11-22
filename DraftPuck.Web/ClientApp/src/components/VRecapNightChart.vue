<script setup lang="ts">
import { computed, ref } from 'vue'
import { format } from 'date-fns'
import VShowMore from '@/components/VShowMore.vue'
import { drinksWord } from '@/helpers/recapHelpers'
import { stretchLabel, type ChartBar, type ChartDrink } from '@/helpers/statsHelpers'

/**
 * The night as a bar chart: drinks handed out every 10 minutes, the selected member's part highlighted. Tapping a bar
 * lists who gave whom a drink in that stretch (the busiest one to start with).
 */
const props = defineProps<{
  bars: ChartBar[]
  member?: LobbyMember
}>()

const tallest = computed(() => Math.max(1, ...props.bars.map((b) => b.total)))
const tappedIndex = ref<number>()
const busiestIndex = computed(() => props.bars.reduce((best, bar, i) => (bar.total > props.bars[best].total ? i : best), 0))
const shownIndex = computed(() => tappedIndex.value ?? busiestIndex.value)
const shown = computed(() => props.bars[shownIndex.value])

const pct = (value: number) => `${Math.round((value / tallest.value) * 100)}%`
const isMine = (drink: ChartDrink) => drink.giver.id === props.member?.id || drink.recipient.id === props.member?.id
</script>

<template>
  <section class="card">
    <h3 class="card-title mb-1">The night</h3>
    <p class="subtitle">Drinks handed out, every 10 minutes. Tap a bar to see who.</p>
    <div class="legend">
      <span><i class="key mine"></i>Involving {{ member?.name }}</span>
      <span><i class="key rest"></i>Everyone else</span>
    </div>
    <div class="plot">
      <!-- The scale: the tallest bar's count at the top line, 0 at the baseline. -->
      <div class="scale">
        <span>{{ tallest }}</span>
        <span>0</span>
      </div>
      <div class="chart" role="list">
        <span class="gridline"></span>
        <button
          v-for="(bar, i) in bars"
          :key="Number(bar.start)"
          type="button"
          role="listitem"
          class="column"
          :class="{ tapped: i === shownIndex }"
          :aria-label="`${stretchLabel(bar)}: ${bar.total} ${drinksWord(bar.total)}, ${bar.involving} involving ${member?.name}`"
          @click="tappedIndex = i"
        >
          <span class="stack" :style="{ height: pct(bar.total) }">
            <span v-if="bar.total - bar.involving" class="seg rest" :style="{ flexGrow: bar.total - bar.involving }"></span>
            <span v-if="bar.involving" class="seg mine" :style="{ flexGrow: bar.involving }"></span>
          </span>
        </button>
      </div>
    </div>
    <div class="axis">
      <span>{{ format(bars[0].start, 'h:mm') }}</span>
      <span>{{ format(bars[bars.length - 1].end, 'h:mm') }}</span>
    </div>

    <!-- The stretch shown: who gave whom a drink, and for which player. -->
    <div v-if="shown" class="stretch">
      <div class="stretch-head">
        <b>{{ stretchLabel(shown) }}</b><span>{{ shown.total }} {{ drinksWord(shown.total) }}</span>
      </div>
      <p v-if="!shown.drinks.length" class="empty">A quiet ten minutes.</p>
      <!-- Keyed by member and stretch, so a long list starts collapsed again for each. -->
      <VShowMore :key="`${member?.id}-${shownIndex}`" :items="shown.drinks" v-slot="{ item: drink }">
        <div class="stretch-drink" :class="{ mine: isMine(drink) }">
          <span class="who">
            <b>{{ drink.giver.name }}</b>
            <span class="arrow">→</span>
            <b>{{ drink.recipient.name }}</b>
          </span>
          <small v-if="drink.player">{{ drink.player }}</small>
        </div>
      </VShowMore>
    </div>
  </section>
</template>

<style scoped lang="scss">
@import '@/assets/scss/recap-cards.scss';

.subtitle {
  margin: 0 0 8px;
  font-size: 13px;
  color: map-get($custom-colors, 'stone-400');
}

.legend {
  display: flex;
  gap: 14px;
  margin-bottom: 10px;
  font-size: 12px;
  color: map-get($custom-colors, 'stone-300');

  span {
    display: flex;
    align-items: center;
    gap: 6px;
  }
}

.key {
  width: 10px;
  height: 10px;
  border-radius: 3px;

  &.mine {
    background: $recap-amber;
  }

  &.rest {
    background: map-get($custom-colors, 'stone-500');
  }
}

.plot {
  display: flex;
  gap: 6px;
}

.scale {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 120px;
  margin-top: -7px;
  margin-bottom: -7px;
  font-size: 11px;
  text-align: right;
  color: map-get($custom-colors, 'stone-400');
}

.chart {
  position: relative;
  flex: 1;
  display: flex;
  align-items: flex-end;
  gap: 3px;
  height: 120px;
  border-bottom: 1px solid map-get($custom-colors, 'stone-600');
}

.gridline {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  border-top: 1px dashed map-get($custom-colors, 'stone-700');
}

// The whole column is the tap target, bigger than the bar.
.column {
  position: relative;
  flex: 1;
  max-width: 26px;
  height: 100%;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 0;
  border: 0;
  border-radius: 4px 4px 0 0;
  background: transparent;

  &.tapped {
    background: rgba(255, 255, 255, 0.07);
  }
}

.stack {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 2px;
  transition: height 0.4s ease-out;
}

.seg {
  flex-basis: 0;
  min-height: 3px;

  &:first-child {
    border-radius: 4px 4px 0 0;
  }

  &.mine {
    background: $recap-amber;
  }

  &.rest {
    background: map-get($custom-colors, 'stone-500');
  }
}

.column.tapped .seg.rest {
  background: map-get($custom-colors, 'stone-300');
}

.axis {
  display: flex;
  justify-content: space-between;
  margin: 4px 0 0 18px;
  font-size: 11px;
  color: map-get($custom-colors, 'stone-400');
}

.stretch {
  margin-top: 12px;
  padding-top: 10px;
  border-top: 1px solid map-get($custom-colors, 'stone-700');
}

.stretch-head {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
  font-size: 13px;
  color: map-get($custom-colors, 'stone-400');

  b {
    color: white;
  }
}

.stretch-drink {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  padding: 4px 0 4px 10px;
  border-left: 2px solid map-get($custom-colors, 'stone-600');
  font-size: 14px;
  color: map-get($custom-colors, 'stone-300');

  & + .stretch-drink {
    margin-top: 4px;
  }

  &.mine {
    border-left-color: $recap-amber;
    color: white;
  }

  .who {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .arrow {
    margin: 0 5px;
    color: map-get($custom-colors, 'stone-400');
  }

  small {
    flex: none;
    font-size: 12px;
    color: map-get($custom-colors, 'stone-400');
  }
}

@media (prefers-reduced-motion: reduce) {
  .stack {
    transition: none;
  }
}
</style>
