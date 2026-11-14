<script setup lang="ts">
// The site logo. The one place that decides which logo to show, so event logos (e.g. for a tournament) can be
// swapped in here without touching the pages that show it.
const src = '/img/logo-wide.png'
</script>

<template>
  <img :src="src" alt="DRAFTPUCK" />
</template>
