<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { format } from 'date-fns'
import { memberNight, nightChart, type ChartBar } from '@/helpers/statsHelpers'

const props = defineProps<{
  lobby: Lobby
  players: RecapPlayer[]
  /** Whose night to show first: the viewer's, when they were in the lobby. */
  initialMemberId?: string
}>()

const people = computed(() => props.lobby.members.filter((m) => !m.isRemoved))
const selectedId = ref(props.initialMemberId ?? people.value[0]?.id ?? '')
const selected = computed(() => people.value.find((m) => m.id === selectedId.value))
const night = computed(() => memberNight(props.lobby, props.players, selectedId.value))
const bars = computed(() => nightChart(props.lobby, props.players, selectedId.value))
const tallest = computed(() => Math.max(1, ...bars.value.map((b) => b.total)))

// The stretch shown under the chart: the one tapped, or the busiest to start with.
const tappedIndex = ref<number>()
const busiestIndex = computed(() => bars.value.reduce((best, bar, i) => (bar.total > bars.value[best].total ? i : best), 0))
const shownIndex = computed(() => tappedIndex.value ?? busiestIndex.value)
const shown = computed(() => bars.value[shownIndex.value])

// Long lists show their top few, with View more / View less; they close again for another person.
const TOP = 3
const expanded = reactive(new Set<string>())
watch(selectedId, () => expanded.clear())
const top = <T,>(list: T[], key: string) => (expanded.has(key) ? list : list.slice(0, TOP))
const toggle = (key: string) => (expanded.has(key) ? expanded.delete(key) : expanded.add(key))

const span = (bar: ChartBar) => `${format(bar.start, 'h:mm')}–${format(bar.end, 'h:mm')}`
const pct = (value: number) => `${Math.round((value / tallest.value) * 100)}%`
const drinks = (count: number) => (count === 1 ? 'drink' : 'drinks')

function avatar(member: LobbyMember) {
  const hasPicture = !!member.avatarPath && !member.isGuest
  return { style: hasPicture ? { backgroundImage: `url(${member.avatarPath})` } : {}, initial: hasPicture ? '' : member.name.charAt(0).toUpperCase() }
}
</script>

<template>
  <div class="stats">
    <!-- Whose night: tap anyone. -->
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
        <span class="avatar" :style="avatar(m).style">{{ avatar(m).initial }}</span>
        <span class="person-name">{{ m.name }}</span>
      </button>
    </div>

    <template v-if="selected">
      <section class="card">
        <h3 class="card-title">{{ selected.name }}'s picks</h3>
        <p v-if="!night.picks.length" class="empty">No picks.</p>
        <div v-for="pick in top(night.picks, 'picks')" :key="pick.name" class="stat-row" :class="{ quiet: !pick.drinks }">
          <span class="row-label"
            ><b>{{ pick.name }}</b
            ><small>{{ pick.team }}</small></span
          >
          <span class="count"
            ><b>{{ pick.drinks }}</b> {{ drinks(pick.drinks) }}</span
          >
        </div>
        <button v-if="night.picks.length > TOP" type="button" class="more" @click="toggle('picks')">
          {{ expanded.has('picks') ? 'View less' : `View more (${night.picks.length - TOP})` }}
        </button>
      </section>

      <section class="card">
        <h3 class="card-title">{{ selected.name }} gave drinks to</h3>
        <p v-if="!night.gaveTo.length" class="empty">Nobody. Merciful.</p>
        <div v-for="c in top(night.gaveTo, 'gave')" :key="c.member.id" class="stat-row">
          <span class="row-label"
            ><span class="avatar sm" :style="avatar(c.member).style">{{ avatar(c.member).initial }}</span
            ><b>{{ c.member.name }}</b></span
          >
          <span class="count"
            ><b>{{ c.count }}</b> {{ drinks(c.count) }}</span
          >
        </div>
        <button v-if="night.gaveTo.length > TOP" type="button" class="more" @click="toggle('gave')">
          {{ expanded.has('gave') ? 'View less' : `View more (${night.gaveTo.length - TOP})` }}
        </button>

        <h3 class="card-title mt-3">{{ selected.name }} received drinks from</h3>
        <p v-if="!night.gotFrom.length" class="empty">Nobody. Untouchable.</p>
        <div v-for="c in top(night.gotFrom, 'got')" :key="c.member.id" class="stat-row">
          <span class="row-label"
            ><span class="avatar sm" :style="avatar(c.member).style">{{ avatar(c.member).initial }}</span
            ><b>{{ c.member.name }}</b></span
          >
          <span class="count taken"
            ><b>{{ c.count }}</b> {{ drinks(c.count) }}</span
          >
        </div>
        <button v-if="night.gotFrom.length > TOP" type="button" class="more" @click="toggle('got')">
          {{ expanded.has('got') ? 'View less' : `View more (${night.gotFrom.length - TOP})` }}
        </button>
      </section>
    </template>

    <section v-if="bars.length" class="card">
      <h3 class="card-title mb-1">The night</h3>
      <p class="subtitle">Drinks handed out, every 10 minutes. Tap a bar to see who.</p>
      <div class="legend">
        <span><i class="key mine"></i>Involving {{ selected?.name }}</span>
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
            :aria-label="`${span(bar)}: ${bar.total} ${drinks(bar.total)}, ${bar.involving} involving ${selected?.name}`"
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
          <b>{{ span(shown) }}</b><span>{{ shown.total }} {{ drinks(shown.total) }}</span>
        </div>
        <p v-if="!shown.drinks.length" class="empty">A quiet ten minutes.</p>
        <div
          v-for="(d, i) in top(shown.drinks, `stretch-${shownIndex}`)"
          :key="i"
          class="stretch-drink"
          :class="{ mine: d.giver.id === selectedId || d.recipient.id === selectedId }"
        >
          <span class="who">
            <b>{{ d.giver.name }}</b>
            <span class="arrow">→</span>
            <b>{{ d.recipient.name }}</b>
          </span>
          <small v-if="d.player">{{ d.player }}</small>
        </div>
        <button v-if="shown.drinks.length > TOP" type="button" class="more" @click="toggle(`stretch-${shownIndex}`)">
          {{ expanded.has(`stretch-${shownIndex}`) ? 'View less' : `View more (${shown.drinks.length - TOP})` }}
        </button>
      </div>
    </section>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

$amber: map-get($custom-colors, 'primary');
$red: #ff5c6c;

.stats {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

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

  .avatar {
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

    .avatar {
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

.avatar {
  flex: none;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 800;
  line-height: 1;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;

  &.sm {
    width: 22px;
    height: 22px;
    font-size: 10px;
    margin-right: 8px;
  }
}

.card {
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px solid map-get($custom-colors, 'stone-700');
  background: linear-gradient(160deg, map-get($custom-colors, 'stone-800'), map-get($custom-colors, 'stone-850') 60%);
}

.card-title {
  margin: 0 0 6px;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: map-get($custom-colors, 'stone-400');
}

.subtitle {
  margin: 0 0 8px;
  font-size: 13px;
  color: map-get($custom-colors, 'stone-400');
}

.empty {
  margin: 0;
  color: map-get($custom-colors, 'stone-400');
  font-size: 14px;
}

.stat-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 0;

  & + .stat-row {
    border-top: 1px solid rgba(255, 255, 255, 0.05);
  }

  &.quiet {
    opacity: 0.45;
  }
}

.row-label {
  flex: 1;
  display: flex;
  align-items: center;
  min-width: 0;
  white-space: nowrap;

  // Long names trail off rather than being cut mid-letter.
  b {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  small {
    flex: none;
    margin-left: 6px;
    font-size: 11px;
    font-weight: 700;
    color: map-get($custom-colors, 'stone-400');
  }
}

// The count, right-aligned in one column: a big number and "drinks".
.count {
  flex: none;
  min-width: 72px;
  text-align: right;
  font-size: 13px;
  color: map-get($custom-colors, 'stone-400');

  b {
    font-size: 18px;
    font-weight: 800;
    font-variant-numeric: tabular-nums;
    color: $amber;
  }

  &.taken b {
    color: $red;
  }
}

.quiet .count b {
  color: map-get($custom-colors, 'stone-400');
}

.more {
  display: block;
  margin: 6px auto 0;
  padding: 4px 10px;
  border: 0;
  background: none;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: $amber;
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
    background: $amber;
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
    background: $amber;
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
    border-left-color: $amber;
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
  .stack,
  .person .avatar {
    transition: none;
  }
}
</style>
