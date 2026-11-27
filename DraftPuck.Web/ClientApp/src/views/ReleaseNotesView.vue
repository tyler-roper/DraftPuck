<script setup lang="ts">
import VIcon from '@/components/VIcon.vue'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import { releaseMonthName, releases } from '@/helpers/releaseNotes'
</script>

<template>
  <HeaderLayout title="Release Notes" :show-save="false" back-fallback="/">
    <div class="notes p-3">
      <article v-for="release in releases" :key="release.month" class="release">
        <header class="mb-3">
          <span class="d-block month">{{ releaseMonthName(release.month) }}</span>
          <h2 class="release-title">{{ release.title }}</h2>
          <p class="summary">{{ release.summary }}</p>
        </header>

        <section v-for="section in release.sections" :key="section.title" class="section">
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
      </article>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.notes {
  max-width: 640px;
  margin: 0 auto;
  color: map-get($custom-colors, 'stone-300');
}

.release + .release {
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid map-get($custom-colors, 'stone-700');
}

.month {
  font-size: 0.8rem;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: map-get($custom-colors, 'stone-400');
}

.release-title {
  margin: 2px 0 8px;
  font-size: 1.5rem;
  font-weight: 800;
  text-transform: uppercase;
  color: map-get($custom-colors, 'stone-0');
}

.summary {
  margin: 0;
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
