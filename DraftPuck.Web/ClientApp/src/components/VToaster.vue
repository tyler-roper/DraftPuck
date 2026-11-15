<script setup lang="ts">
import { useToastStack, type Toast, type ToastType } from '@/composables/useToast'

const { toasts, dismiss, pause, resume } = useToastStack()

const typeIcons: Record<ToastType, string | undefined> = {
  default: undefined,
  info: 'fi fi-sr-info',
  success: 'fi fi-sr-check-circle',
  error: 'fi fi-sr-cross-circle'
}

function onToastClick(item: Toast) {
  item.onClick?.()
  dismiss(item.id)
}
</script>

<template>
  <!-- Newest at the bottom; the rest slide up to make room. -->
  <TransitionGroup tag="div" name="toast" class="toaster">
    <div
      v-for="item in toasts"
      :key="item.id"
      class="toast-card"
      :class="`toast-${item.type}`"
      :role="item.type === 'error' ? 'alert' : 'status'"
      @click="onToastClick(item)"
      @mouseenter="pause(item.id)"
      @mouseleave="resume(item.id)"
    >
      <i v-if="item.icon ?? typeIcons[item.type]" class="toast-icon" :class="item.icon ?? typeIcons[item.type]" aria-hidden="true"></i>

      <div class="toast-body">
        <template v-if="item.isHtml">
          <span v-if="item.title" class="toast-title" v-html="item.title"></span>
          <div v-html="item.message"></div>
        </template>
        <div v-else>{{ item.message }}</div>
      </div>

      <button type="button" class="toast-close" aria-label="Close" @click.stop="dismiss(item.id)">&times;</button>

      <div v-if="item.timeout !== false" class="toast-countdown" :style="{ animationDuration: `${item.timeout}ms` }"></div>
    </div>
  </TransitionGroup>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.toaster {
  position: fixed;
  z-index: 9999;
  bottom: 1em;
  left: 50%;
  transform: translateX(-50%);
  width: 600px;
  max-width: 100vw;
  padding: 4px;
  display: flex;
  flex-direction: column;
  align-items: center;
  pointer-events: none;
}

.toast-card {
  --toast-accent: #{map-get($custom-colors, 'primary')};

  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  width: 100%;
  min-height: 56px;
  margin-top: 0.75rem;
  padding: 14px 16px 16px;
  overflow: hidden;
  pointer-events: auto;
  cursor: pointer;
  color: #{map-get($custom-colors, 'stone-0')};
  background-color: #{map-get($custom-colors, 'stone-800')};
  border: 1px solid #{map-get($custom-colors, 'stone-700')};
  border-left: 4px solid var(--toast-accent);
  border-radius: 10px;
  box-shadow: 0 0 10px rgba(0, 0, 0, 0.6);
}

.toast-info {
  --toast-accent: #{map-get($custom-colors, 'light-blue')};
}

.toast-success {
  --toast-accent: #4caf50;
}

.toast-error {
  --toast-accent: #{map-get($custom-colors, 'red')};
}

.toast-icon {
  display: flex;
  font-size: 1.5rem;
  color: var(--toast-accent);
}

.toast-body {
  flex: 1;
  font-size: 0.95rem;
  line-height: 1.4;
  white-space: pre-wrap;
  word-break: break-word;
}

.toast-title {
  display: block;
  margin-bottom: 2px;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.toast-close {
  align-self: flex-start;
  padding: 0 0 0 6px;
  font-size: 1.4rem;
  font-weight: 700;
  line-height: 1;
  color: inherit;
  background: transparent;
  border: none;
  opacity: 0.4;
  transition: opacity 0.2s linear;

  &:hover,
  &:focus-visible {
    opacity: 1;
  }
}

// Shrinks over the toast's timeout and stops while hovered, in step with the countdown in useToast.
.toast-countdown {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 3px;
  background-color: var(--toast-accent);
  opacity: 0.7;
  transform-origin: left;
  animation: toast-countdown linear forwards;
}

.toast-card:hover .toast-countdown {
  animation-play-state: paused;
}

@keyframes toast-countdown {
  from {
    transform: scaleX(1);
  }
  to {
    transform: scaleX(0);
  }
}

// The same entrance, exit and shuffle as vue-toastification's bottom-center toasts.
.toast-enter-active,
.toast-leave-active {
  animation-duration: 750ms;
  animation-fill-mode: both;
}

.toast-enter-active {
  animation-name: toast-bounce-in-up;
}

.toast-leave-active {
  animation-name: toast-bounce-out-down;
}

.toast-move {
  transition: all 400ms ease-in-out;
}

@keyframes toast-bounce-in-up {
  from,
  60%,
  75%,
  90%,
  to {
    animation-timing-function: cubic-bezier(0.215, 0.61, 0.355, 1);
  }
  from {
    opacity: 0;
    transform: translate3d(0, 3000px, 0);
  }
  60% {
    opacity: 1;
    transform: translate3d(0, -20px, 0);
  }
  75% {
    transform: translate3d(0, 10px, 0);
  }
  90% {
    transform: translate3d(0, -5px, 0);
  }
  to {
    transform: translate3d(0, 0, 0);
  }
}

@keyframes toast-bounce-out-down {
  20% {
    transform: translate3d(0, 10px, 0);
  }
  40%,
  45% {
    opacity: 1;
    transform: translate3d(0, -20px, 0);
  }
  to {
    opacity: 0;
    transform: translate3d(0, 2000px, 0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .toast-enter-active,
  .toast-leave-active {
    animation: none;
  }

  .toast-move {
    transition: none;
  }
}

@media only screen and (max-width: 600px) {
  .toaster {
    bottom: 0;
    padding: 8px;
  }
}
</style>
