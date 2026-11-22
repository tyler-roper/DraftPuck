<script setup lang="ts">
import VRecapAvatar from '@/components/VRecapAvatar.vue'
import VShowMore from '@/components/VShowMore.vue'
import { drinksWord } from '@/helpers/recapHelpers'
import type { MemberNight } from '@/helpers/statsHelpers'

/** One member's night on the Stats tab: their picks' drinks, and who they gave drinks to and got them from. */
defineProps<{
  member: LobbyMember
  night: MemberNight
}>()
</script>

<template>
  <section class="card">
    <h3 class="card-title">{{ member.name }}'s picks</h3>
    <p v-if="!night.picks.length" class="empty">No picks.</p>
    <VShowMore :items="night.picks" v-slot="{ item: pick }">
      <div class="stat-row" :class="{ quiet: !pick.drinks }">
        <span class="row-label"
          ><b>{{ pick.name }}</b
          ><small>{{ pick.team }}</small></span
        >
        <span class="count"
          ><b>{{ pick.drinks }}</b> {{ drinksWord(pick.drinks) }}</span
        >
      </div>
    </VShowMore>
  </section>

  <section class="card">
    <h3 class="card-title">{{ member.name }} gave drinks to</h3>
    <p v-if="!night.gaveTo.length" class="empty">Nobody. Merciful.</p>
    <VShowMore :items="night.gaveTo" v-slot="{ item }">
      <div class="stat-row">
        <span class="row-label"
          ><VRecapAvatar :member="item.member" class="row-avatar" /><b>{{ item.member.name }}</b></span
        >
        <span class="count"
          ><b>{{ item.count }}</b> {{ drinksWord(item.count) }}</span
        >
      </div>
    </VShowMore>

    <h3 class="card-title mt-3">{{ member.name }} received drinks from</h3>
    <p v-if="!night.gotFrom.length" class="empty">Nobody. Untouchable.</p>
    <VShowMore :items="night.gotFrom" v-slot="{ item }">
      <div class="stat-row">
        <span class="row-label"
          ><VRecapAvatar :member="item.member" class="row-avatar" /><b>{{ item.member.name }}</b></span
        >
        <span class="count taken"
          ><b>{{ item.count }}</b> {{ drinksWord(item.count) }}</span
        >
      </div>
    </VShowMore>
  </section>
</template>

<style scoped lang="scss">
@import '@/assets/scss/recap-cards.scss';

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

.row-avatar {
  width: 22px;
  height: 22px;
  font-size: 10px;
  margin-right: 8px;
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
    color: $recap-amber;
  }

  &.taken b {
    color: $recap-red;
  }
}

.quiet .count b {
  color: map-get($custom-colors, 'stone-400');
}
</style>
