<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { format } from 'date-fns'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import VInputWrapper from '@/components/VInputWrapper.vue'
import { useToast } from '@/composables/useToast'
import AdminEventLogoService from '@/services/admin/AdminEventLogoService'
import { getErrorMessage } from '@/helpers/requestHelpers'
import {
  eventLogoFormError,
  eventLogoStatus,
  isLogoUrl,
  toCreateEventLogoRequest,
  type AdminEventLogo,
  type EventLogoForm
} from '@/helpers/eventLogoHelpers'

/** Schedules the logos shown instead of the usual one during events. Visits that start during an event see its logo. */
const toast = useToast()
const logos = ref<AdminEventLogo[]>([])
const isLoading = ref(true)
const isSaving = ref(false)
const now = new Date()

const emptyForm = (): EventLogoForm => ({ name: '', starts: '', ends: '', logoUrl: '', wideLogoUrl: '' })
const form = reactive<EventLogoForm>(emptyForm())
const formError = computed(() => eventLogoFormError(form))

async function load() {
  try {
    logos.value = await AdminEventLogoService.getAll()
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't load the event logos."))
  } finally {
    isLoading.value = false
  }
}

async function add() {
  if (formError.value) return toast.error(formError.value)
  isSaving.value = true
  try {
    await AdminEventLogoService.create(toCreateEventLogoRequest(form))
    toast.success(`${form.name.trim()} added.`)
    Object.assign(form, emptyForm())
    await load()
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't add the event logo."))
  } finally {
    isSaving.value = false
  }
}

async function remove(logo: AdminEventLogo) {
  if (!confirm(`Remove the ${logo.name} logo?`)) return
  try {
    await AdminEventLogoService.remove(logo.id)
    logos.value = logos.value.filter((l) => l.id !== logo.id)
  } catch (ex) {
    toast.error(getErrorMessage(ex, "Couldn't remove the event logo."))
  }
}

onMounted(load)
</script>

<template>
  <HeaderLayout title="Event Logos" :show-save="false">
    <div class="p-3">
      <p class="text-stone-400">
        A logo replaces the usual one from its start until its end, for visits that begin during the event. When events overlap, the one that started
        latest wins.
      </p>

      <form class="panel mb-4" @submit.prevent="add">
        <span class="d-block fw-bold mb-3">Add an event</span>
        <label class="form-label" for="eventName">Name</label>
        <VInputWrapper class="mb-3">
          <input id="eventName" v-model="form.name" class="form-control dark" maxlength="100" placeholder="Winter Classic 2027" />
        </VInputWrapper>
        <label class="form-label" for="eventStarts">Starts <span class="fs-7 text-stone-400">(your local time)</span></label>
        <VInputWrapper class="mb-3">
          <input id="eventStarts" v-model="form.starts" type="datetime-local" class="form-control dark" />
        </VInputWrapper>
        <label class="form-label" for="eventEnds">Ends</label>
        <VInputWrapper class="mb-3">
          <input id="eventEnds" v-model="form.ends" type="datetime-local" class="form-control dark" />
        </VInputWrapper>
        <label class="form-label" for="eventLogo">Logo (splash and home pages)</label>
        <VInputWrapper class="mb-2">
          <input id="eventLogo" v-model="form.logoUrl" class="form-control dark" placeholder="/img/logo-event.png" />
        </VInputWrapper>
        <img v-if="isLogoUrl(form.logoUrl)" :src="form.logoUrl.trim()" alt="" class="preview square mb-3" />
        <label class="form-label" for="eventWideLogo">Wide logo (lobby header)</label>
        <VInputWrapper class="mb-2">
          <input id="eventWideLogo" v-model="form.wideLogoUrl" class="form-control dark" placeholder="/img/logo-wide-event.png" />
        </VInputWrapper>
        <img v-if="isLogoUrl(form.wideLogoUrl)" :src="form.wideLogoUrl.trim()" alt="" class="preview wide mb-3" />
        <VButton type="submit" class="btn-primary fw-bold w-100 mt-2" :is-loading="isSaving">Add event logo</VButton>
      </form>

      <span class="d-block fw-bold mb-2">Scheduled</span>
      <div v-if="isLoading" class="text-stone-400">Loading…</div>
      <div v-else-if="logos.length === 0" class="text-stone-400">No event logos yet.</div>
      <div v-for="logo in logos" :key="logo.id" class="panel mb-2">
        <div class="d-flex align-items-start gap-2">
          <div class="flex-grow-1 min-w-0">
            <span class="d-block fw-bold text-truncate">{{ logo.name }}</span>
            <span class="status" :class="eventLogoStatus(logo, now).replace(' ', '-').toLowerCase()">{{ eventLogoStatus(logo, now) }}</span>
            <span class="d-block fs-7 text-stone-400 mt-1">{{ format(logo.startsUtc, 'PP p') }} – {{ format(logo.endsUtc, 'PP p') }}</span>
          </div>
          <button class="btn btn-sm text-stone-400" title="Remove" :aria-label="`Remove ${logo.name}`" @click="remove(logo)">
            <VIcon prefix="rr" icon="trash" />
          </button>
        </div>
        <div class="d-flex align-items-center gap-3 mt-2">
          <img :src="logo.logoUrl" alt="" class="preview square" />
          <img :src="logo.wideLogoUrl" alt="" class="preview wide min-w-0" />
        </div>
      </div>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.panel {
  padding: 12px;
  border-radius: 12px;
  border: 1px solid map-get($custom-colors, 'stone-700');
  background-color: map-get($custom-colors, 'stone-850');
}

.preview {
  display: block;
  object-fit: contain;
  background-color: map-get($custom-colors, 'stone-1000');
  border-radius: 8px;
}

.preview.square {
  width: 64px;
  height: 64px;
}

.preview.wide {
  height: 40px;
  max-width: 100%;
}

.status {
  display: inline-block;
  margin-top: 4px;
  padding: 1px 8px;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: bold;
  background-color: map-get($custom-colors, 'stone-700');
  color: map-get($custom-colors, 'stone-300');
}

.status.on-now {
  background-color: map-get($custom-colors, 'primary');
  color: map-get($custom-colors, 'stone-900');
}

.min-w-0 {
  min-width: 0;
}
</style>
