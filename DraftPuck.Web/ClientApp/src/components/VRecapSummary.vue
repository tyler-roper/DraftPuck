<script setup lang="ts">
import CountUp from 'vue-countup-v3'
import type { recapTotals } from '@/helpers/recapHelpers'

/** The top of the post-game page: the lobby, its date, and the night's totals counting up. */
defineProps<{
  joinCode: string
  dateLabel: string
  totals: ReturnType<typeof recapTotals>
}>()

const totalLabels = [
  ['games', 'Games'],
  ['players', 'Players'],
  ['picks', 'Picks'],
  ['drinks', 'Drinks']
] as const
</script>

<template>
  <div>
    <div class="d-flex justify-content-between bg-stone-900 px-3 py-2 bg-gradient">
      <div>
        <span class="d-block fs-9 fw-bold text-uppercase mb-n1 text-stone-400">Lobby</span>
        <span class="d-block fs-5 fw-bold ls-5">{{ joinCode }}</span>
      </div>
      <div>
        <span class="d-block fs-9 fw-bold text-uppercase mb-n1 text-stone-400 text-end">Date</span>
        <span class="d-block fs-5 fw-bold text-end">{{ dateLabel }}</span>
      </div>
    </div>
    <div class="d-flex justify-content-between pt-3 bg-stone-900 px-3 pb-2">
      <div v-for="[key, label] in totalLabels" :key="key" class="text-center">
        <span class="d-block fs-7 fw-bold mb-n2 text-uppercase text-primary">{{ label }}</span>
        <span class="d-block fs-1 fw-bold">
          <CountUp :end-val="totals[key]" :duration="3"></CountUp>
        </span>
      </div>
    </div>
  </div>
</template>
