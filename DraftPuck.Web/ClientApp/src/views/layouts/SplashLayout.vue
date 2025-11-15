<script setup lang="ts">
withDefaults(
  defineProps<{
    showHeaderText?: boolean
    showFlavorText?: boolean
    increaseContrast?: boolean
    alignToBottom?: boolean,
    padding?: number
  }>(),
  {
    showHeaderText: true,
    showFlavorText: true,
    increaseContrast: false,
    alignToBottom: true,
    padding: 5
  }
)
</script>

<template>
  <div class="page container-fluid" :class="{ 'increase-contrast': increaseContrast }">
    <div class="content d-flex flex-column" :class="`p-${padding}`">
      <header class="p-5" :class="`m-n${padding}`">
        <img src="/img/logo.png" class="w-100" />
      </header>
      <div class="mt-3">
        <div class="splash-text text-amber-400 text-center" v-if="showHeaderText">
          <span>Score One,</span>
          <span class="d-block text-white">Pour One</span>
        </div>
        <p class="mt-4 rubik fs-6 fw-bold text-center text-stone-200" v-if="showFlavorText">
          Draft NHL players in real time. If they score, your friends drink.<br><br>Fantasy hockey - <i>real punishment</i>.
        </p>
      </div>
      <div :class="{'mt-auto': alignToBottom}">
        <slot></slot>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.page {
  height: 100dvh;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(map-get($custom-colors, 'stone-1000'), 0.2), map-get($custom-colors, 'stone-1000') 75%), url(/img/net.jpg);
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center center;
  padding: 0 !important;
  display: flex;
  flex-direction: column;
}

.page > .content {
  height: 100%;
  max-width: 550px;
  margin: 0 auto;
  overflow-y: auto;
  overflow-x: hidden;
}

.page.increase-contrast {
  background: linear-gradient(180deg, rgba(map-get($custom-colors, 'stone-1000'), 0.2), map-get($custom-colors, 'stone-1000') 45%), url(/img/net.jpg);
}

.splash-text {
  margin-top: 100px;
  font-size: 4.2rem;
  line-height: 3.75rem;
  font-family: 'Rubik';
  font-weight: 700;
  text-transform: uppercase;
  margin-left: -50px;
  margin-right: -50px;
}

.rubik {
  font-family: 'Rubik';
}
</style>
