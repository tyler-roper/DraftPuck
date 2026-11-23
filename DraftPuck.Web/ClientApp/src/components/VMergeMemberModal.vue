<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
import { describeSpotContents, mergeCandidates, spotContents } from '@/helpers/memberMergeHelpers'

/**
 * The host says a member is a guest who joined again (new tab, new phone): they choose the spot that guest had before,
 * see which way the merge goes, and confirm. The original spot stays; the duplicate is removed.
 */
const props = defineProps<{ duplicate: LobbyMember; isMerging?: boolean }>()
const emit = defineEmits<{ merge: [original: LobbyMember]; close: [] }>()

const { lobby } = storeToRefs(useLobbyStore())
const candidates = computed(() => mergeCandidates(lobby.value!, props.duplicate))
const originalId = ref(candidates.value.length === 1 ? candidates.value[0].id : undefined)
const original = computed(() => candidates.value.find((m) => m.id === originalId.value))

const kept = computed(() => (original.value ? describeSpotContents(spotContents(lobby.value!, original.value)) : ''))
const dropped = computed(() => describeSpotContents(spotContents(lobby.value!, props.duplicate)))
</script>

<template>
  <div class="modal-overlay" @click="!isMerging && emit('close')">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block text-truncate">Is {{ duplicate.name }} someone already here?</span>
        <a v-if="!isMerging" role="button" class="text-uppercase text-primary text-decoration-none ms-3" @click="emit('close')">Close</a>
      </div>

      <div class="modal-body dark">
        <p class="mb-2">
          If <strong>{{ duplicate.name }}</strong> is a guest who joined again from a new tab or phone, choose the spot they had before.
        </p>

        <div class="d-flex flex-wrap gap-2">
          <button
            v-for="candidate in candidates"
            :key="candidate.id"
            type="button"
            class="chip"
            :class="{ chosen: candidate.id === originalId }"
            :disabled="isMerging"
            @click="originalId = candidate.id"
          >
            {{ candidate.name }}
          </button>
        </div>

        <template v-if="original">
          <div class="merge-direction mt-3" aria-label="Which way the merge goes">
            <div class="spot removed">
              <span class="spot-label">Removed</span>
              <strong class="spot-name">{{ duplicate.name }}</strong>
              <span class="spot-contents">{{ dropped }}</span>
            </div>
            <i class="fi fi-sr-arrow-right arrow" aria-hidden="true"></i>
            <div class="spot kept">
              <span class="spot-label">Kept</span>
              <strong class="spot-name">{{ original.name }}</strong>
              <span class="spot-contents">{{ kept }}</span>
            </div>
          </div>

          <ul class="consequences mt-3 mb-0">
            <li>
              Whoever is playing as <strong>{{ duplicate.name }}</strong> becomes <strong>{{ original.name }}</strong><template
                v-if="kept !== 'nothing yet'"
                >, with {{ original.name }}'s {{ kept }}</template
              >.
            </li>
            <li v-if="dropped === 'nothing yet'">
              <strong>{{ duplicate.name }}</strong> is removed. It hasn't picked or drunk anything.
            </li>
            <li v-else>
              <strong>{{ duplicate.name }}</strong> is removed, and its {{ dropped }} go with it.
            </li>
            <li>
              The old tab or phone that was playing as <strong>{{ original.name }}</strong> leaves the lobby.
            </li>
          </ul>

          <button type="button" class="btn btn-primary fw-bold w-100 mt-3 py-2" :disabled="isMerging" @click="emit('merge', original)">
            {{ isMerging ? 'Merging…' : `Move ${duplicate.name} into ${original.name}'s spot` }}
          </button>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Dark, like the rest of the app (the shared modal body is white).
.modal-body.dark {
  background: map-get($custom-colors, 'stone-850');
  color: map-get($custom-colors, 'stone-0');
}

.chip {
  padding: 4px 12px;
  font-size: 0.9rem;
  font-weight: 700;
  border-radius: 999px;
  border: 1px solid map-get($custom-colors, 'stone-600');
  background: map-get($custom-colors, 'stone-800');
  color: map-get($custom-colors, 'stone-100');
}

.chip.chosen {
  border-color: map-get($custom-colors, 'amber-500');
  background: map-get($custom-colors, 'amber-500');
  color: map-get($custom-colors, 'stone-900');
}

// Removed → Kept, side by side even on a phone: the direction is the point.
.merge-direction {
  display: flex;
  align-items: stretch;
  gap: 8px;
}

.spot {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  padding: 8px 10px;
  border-radius: 8px;
  border: 2px solid;
}

.spot.removed {
  border-color: map-get($custom-colors, 'stone-600');
  background: map-get($custom-colors, 'stone-800');
}

.spot.removed .spot-name {
  text-decoration: line-through;
  color: map-get($custom-colors, 'stone-400');
}

.spot.kept {
  border-color: map-get($custom-colors, 'amber-500');
  background: map-get($custom-colors, 'stone-800');
}

.spot-label {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: map-get($custom-colors, 'stone-400');
}

.spot.kept .spot-label {
  color: map-get($custom-colors, 'amber-500');
}

.spot-name {
  font-size: 1.05rem;
  overflow-wrap: anywhere;
}

.spot-contents {
  font-size: 0.8rem;
  color: map-get($custom-colors, 'stone-300');
}

.arrow {
  align-self: center;
  font-size: 1.4rem;
  color: map-get($custom-colors, 'amber-500');
  line-height: 1;
}

.consequences {
  padding-left: 1.1rem;
  font-size: 0.9rem;
  color: map-get($custom-colors, 'stone-200');
}

.consequences li + li {
  margin-top: 4px;
}
</style>
