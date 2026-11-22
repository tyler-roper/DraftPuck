<script setup lang="ts">
import VJoinLobbyForm from '@/components/VJoinLobbyForm.vue'

/** A guest opened a lobby's link without being a member: they choose a name and join. */
defineProps<{
  joinCode: string
}>()

const emit = defineEmits<{ joined: [] }>()
</script>

<template>
  <div class="d-flex align-items-center justify-content-center w-100 p-3">
    <div class="bg-stone-900 rounded shadow p-4" style="width: 100%; max-width: 420px">
      <h2 class="fs-4 fw-bold text-uppercase mb-1">Join lobby {{ joinCode }}</h2>
      <p class="text-stone-400 mb-3">Choose a name to join as a guest.</p>
      <VJoinLobbyForm :code="joinCode" @joined="emit('joined')" />
    </div>
  </div>
</template>
