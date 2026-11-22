<script setup lang="ts">
import { computed, ref } from 'vue'
import VRecapPeople from '@/components/VRecapPeople.vue'
import VRecapMemberNight from '@/components/VRecapMemberNight.vue'
import VRecapNightChart from '@/components/VRecapNightChart.vue'
import { memberNight, nightChart } from '@/helpers/statsHelpers'

/** The post-game page's Stats tab: pick a member to see their night, and the whole night as a chart. */
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
</script>

<template>
  <div class="stats">
    <VRecapPeople v-model="selectedId" :people="people" />
    <!-- Keyed by member, so long lists start collapsed again for each person. -->
    <VRecapMemberNight v-if="selected" :key="selected.id" :member="selected" :night="night" />
    <VRecapNightChart v-if="bars.length" :bars="bars" :member="selected" />
  </div>
</template>

<style scoped>
.stats {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>
