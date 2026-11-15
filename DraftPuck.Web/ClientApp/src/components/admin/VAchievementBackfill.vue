<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { format } from 'date-fns'
import { useToast } from 'vue-toastification'
import VButton from '@/components/VButton.vue'
import AdminAchievementService from '@/services/admin/AdminAchievementService'
import { backfillProgress, describeBackfill, type AchievementBackfill } from '@/helpers/backfillHelpers'
import { getErrorMessage } from '@/helpers/requestHelpers'

/** Starts an achievement backfill and shows the latest run's progress, refreshing while it runs. */
const toast = useToast()
const run = ref<AchievementBackfill | null>(null)
const isStarting = ref(false)
const isRunning = computed(() => run.value != null && !run.value.isComplete)
let timer: ReturnType<typeof setInterval> | undefined

async function refresh() {
  try {
    run.value = await AdminAchievementService.getLatestBackfill()
  } catch {
    // A missed refresh is fine; the next one catches up.
  }
  if (!isRunning.value) stopPolling()
}

function startPolling() {
  stopPolling()
  timer = setInterval(refresh, 3000)
}

function stopPolling() {
  if (timer) clearInterval(timer)
  timer = undefined
}

async function start() {
  if (!confirm('Check every player for achievements they have already earned? Awards are silent (no notifications).')) return
  isStarting.value = true
  try {
    run.value = await AdminAchievementService.startBackfill()
    toast.success(`Backfill started for ${run.value.usersQueued.toLocaleString()} players.`)
    if (isRunning.value) startPolling()
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't start the backfill."))
  } finally {
    isStarting.value = false
  }
}

onMounted(async () => {
  await refresh()
  if (isRunning.value) startPolling()
})
onUnmounted(stopPolling)
</script>

<template>
  <div>
    <p class="text-stone-400 mb-3">
      Awards achievements players earned before their rules existed or were fixed. Safe to run again: nobody gets an
      achievement twice, and nobody is notified.
    </p>
    <div v-if="run" class="mb-3">
      <div class="d-flex justify-content-between fs-7 text-stone-400 mb-1">
        <span>Last run {{ format(new Date(run.startedUtc), 'PPp') }}</span>
        <span>{{ backfillProgress(run) }}%</span>
      </div>
      <div class="progress mb-2" style="height: 6px">
        <div class="progress-bar" :class="run.isComplete ? 'bg-success' : 'bg-primary progress-bar-striped progress-bar-animated'"
          :style="{ width: `${backfillProgress(run)}%` }"></div>
      </div>
      <span class="d-block fw-bold">{{ describeBackfill(run) }}</span>
    </div>
    <VButton class="btn btn-primary fw-bold w-100" :is-loading="isStarting || isRunning"
      :loading-text="isRunning ? 'Backfill running…' : 'Starting...'" @click="start">Backfill achievements</VButton>
  </div>
</template>
