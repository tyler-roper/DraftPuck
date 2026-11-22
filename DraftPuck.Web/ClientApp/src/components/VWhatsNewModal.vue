<script setup lang="ts">
import VIcon from '@/components/VIcon.vue'
import { whatsNew } from '@/helpers/whatsNew'

const emit = defineEmits(['close'])
const close = () => emit('close')
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block">What's New</span>
        <a role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>

      <div class="modal-body dark">
        <p class="intro">
          Friends, a new post-game page, a redesigned feed, 20+ new achievements, and a lot of work on making live games faster and more reliable,
          especially on iPhone.
        </p>

        <section v-for="section in whatsNew" :key="section.title" class="section">
          <span class="section-title"><VIcon :icon="section.icon" prefix="sr" class="me-2" />{{ section.title }}</span>
          <div v-if="section.images" class="shots">
            <img v-for="shot in section.images" :key="shot.src" :src="shot.src" :alt="shot.alt" loading="lazy" />
          </div>
          <ul>
            <li v-for="item in section.items" :key="item.lead">
              <strong>{{ item.lead }}</strong>{{ item.text ? ` ${item.text}` : '' }}
            </li>
          </ul>
        </section>

        <button class="btn btn-primary fw-bold w-100 py-3 mt-2" @click="close">Let's go</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Above the home page's floating Rejoin bar.
.modal-overlay,
.dp-modal {
  z-index: 1001;
}

// Dark, like How To Play (the shared modal body is white).
.modal-body.dark {
  background: map-get($custom-colors, 'stone-850');
  color: map-get($custom-colors, 'stone-300');
}

.intro {
  margin: 0 0 12px;
  line-height: 1.45;
}

.section {
  padding: 14px;
  margin-bottom: 10px;
  border-radius: 12px;
  background: map-get($custom-colors, 'stone-900');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.section-title {
  display: block;
  margin-bottom: 8px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: map-get($custom-colors, 'primary');
}

// Phone screenshots in a row: two fit, more scroll sideways.
.shots {
  display: flex;
  gap: 10px;
  margin-bottom: 12px;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
}

.shots img {
  flex: 0 0 calc(50% - 5px);
  min-width: 0;
  scroll-snap-align: start;
  border-radius: 10px;
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.shots img:only-child {
  margin: 0 auto;
}

ul {
  margin: 0;
  padding-left: 18px;
}

li {
  margin-bottom: 6px;
  font-size: 0.92rem;
  line-height: 1.45;
}

li:last-child {
  margin-bottom: 0;
}

strong {
  color: map-get($custom-colors, 'stone-0');
}
</style>
