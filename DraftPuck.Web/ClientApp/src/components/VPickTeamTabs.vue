<script setup lang="ts">
import { lightTeamLogoUrl } from '@/helpers/teamLogos'

defineProps<{
  game: Game
  selectedTeamId?: number
}>()

const emit = defineEmits<{ select: [team: GameTeam] }>()
</script>

<template>
  <!-- Away on the left, home on the right (mirrored: its logo on the outside). -->
  <div class="fs-5 d-flex justify-content-between">
    <div
      v-for="team in [game.awayTeam, game.homeTeam]"
      :key="team.id"
      class="p-2 d-flex align-items-center w-50"
      :class="{
        'flex-row-reverse text-end': team.id === game.homeTeam.id,
        'o-75': selectedTeamId !== team.id,
        'bg-stone-700': selectedTeamId === team.id
      }"
      @click="emit('select', team)"
    >
      <div>
        <img style="height: 25px" :src="lightTeamLogoUrl(team)" class="d-block" />
      </div>
      <div class="text-uppercase fs-6">
        <div v-if="team.location.length > 1" class="fs-7 text-stone-300">{{ team.location }}</div>
        <div class="fw-bold" :class="{ 'mt-n2': team.location.length > 1 }">{{ team.name }}</div>
      </div>
    </div>
  </div>
</template>
