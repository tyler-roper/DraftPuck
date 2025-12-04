<script setup lang="ts">
defineProps<{
  senderName: string | undefined
}>()
</script>

<template>
  <div v-if="senderName" class="drink-animation d-flex align-items-center justify-content-center">
    <span class="text-white text-center">
      <span class="text-uppercase fw-bold" style="font-size: 100px">Drink!</span>
      <span class="d-block fs-5" style="opacity: 0.5"> Courtesy Of </span>
      <span class="d-block fw-bold fs-2 text-uppercase">
        {{ senderName }}
      </span>
    </span>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

@keyframes bouncein {
  0% {
    transform: translate(-50%, -50%) scale(0.4);
    opacity: 0;
    animation-timing-function: cubic-bezier(0.34, 1.56, 0.64, 1);
  }

  25% {
    transform: translate(-50%, -50%) scale(1.08);
    opacity: 1;
    animation-timing-function: ease-out;
  }

  100% {
    transform: translate(-50%, -50%) scale(1);
    animation-timing-function: ease-in-out;
  }
}

.drink-animation {
  position: absolute;
  z-index: 99;
  max-width: 100%;
  width: 375px;
  max-height: 100%;
  height: 375px;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background-color: map-get($custom-colors, 'amber-500');
  border-radius: 20px;
  box-shadow: 0 0 25px black;
  animation: bouncein;
  animation-duration: 2.5s;
  animation-iteration-count: 2;
  animation-direction: alternate;
  animation-fill-mode: forwards;
}
</style>
