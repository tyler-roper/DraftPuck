<script setup lang="ts">
import VButton from '@/components/VButton.vue'
import VIcon from '@/components/VIcon.vue'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
withDefaults(
  defineProps<{
    title: string
    isDirty?: boolean
    showSave?: boolean
    isSaving?: boolean
  }>(),
  {
    isDirty: false,
    showSave: true,
    isSaving: false
  }
)

//#region data
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)
const emit = defineEmits(['discard', 'save'])
//#endregion

//#region methods
//#endregion
</script>

<template>
  <div class="d-flex flex-column h-100 bg-dark-gradient overflow-auto">
    <div class="text-center bg-stone-900">
      <div class="position-relative">
        <router-link :to="`/u/${currentUser!.nickname}`" class="back-link" replace>
          <VIcon icon="angle-circle-left" prefix="sr" class="fs-3 text-stone-300" />
        </router-link>
        <h1 class="p-3 m-0 text-uppercase fw-bold ls-2 mx-auto">{{ title }}</h1>
      </div>
      <slot name="header"></slot>
    </div>
    <div class="flex-grow-1 overflow-scroll">
      <slot></slot>
    </div>
    <div class="bg-stone-900">
      <slot name="footer"></slot>
      <div v-if="showSave" class="d-flex p-3 justify-content-between align-items-center overflow-hidden">
        <a v-if="isDirty" @click="emit('discard')" role="btn" class="text-stone-400 fs-7 me-4 ls-2"><span>Discard Changes</span></a>
        <VButton
          @click="emit('save')"
          class="btn btn-primary px-5 ms-auto"
          :disabled="!isDirty || isSaving"
          :is-loading="isSaving"
          :show-text="true"
          loading-text="Saving..."
          >Save Changes</VButton
        >
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';
.back-link {
  position: absolute;
  left: 20px;
  top: 50%;
  transform: translateY(-50%);
}

.bg-dark-gradient {
  background-image: linear-gradient(to bottom, map-get($custom-colors, 'stone-1000'), map-get($custom-colors, 'stone-800'));
}

.overflow-auto {
  overflow: auto;
}
</style>
