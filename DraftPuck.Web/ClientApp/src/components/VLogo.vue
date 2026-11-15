<script setup lang="ts">
import { computed } from 'vue'
import { useSystemStore, type LogoVariant } from '@/stores/system'

// The site logo, swapped for an event's logo while one is on (see the EventLogos table). It's fetched once per visit
// with the site settings, so showing it costs no requests.
const props = withDefaults(defineProps<{ variant?: LogoVariant }>(), { variant: 'wide' })

const systemStore = useSystemStore()
const src = computed(() => systemStore.logoUrl(props.variant))
</script>

<template>
  <img :src="src" alt="DRAFTPUCK" />
</template>
