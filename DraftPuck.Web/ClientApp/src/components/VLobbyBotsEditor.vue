<script setup lang="ts">
import VInputWrapper from '@/components/VInputWrapper.vue'
import VIcon from '@/components/VIcon.vue'
import { BOT_PICK_STYLE_OPTIONS, MAX_BOTS, newBot } from '@/helpers/botHelpers'

defineProps<{
  // Per bot, whether its name failed the form's validation.
  invalidNicknames: boolean[]
}>()

const bots = defineModel<Bot[]>({ required: true })

function addBot() {
  if (bots.value.length < MAX_BOTS) bots.value = [...bots.value, newBot(bots.value)]
}

function removeBot(index: number) {
  bots.value = bots.value.filter((_, i) => i !== index)
}

function updateBot(index: number, change: Partial<Bot>) {
  bots.value = bots.value.map((bot, i) => (i === index ? { ...bot, ...change } : bot))
}
</script>

<template>
  <div class="bot-container">
    <div class="title-bar">
      <span v-if="!bots.length">No Bots</span>
      <span v-else-if="bots.length === 1">1 Bot</span>
      <span v-else>{{ bots.length }} Bots</span>
      <a v-if="bots.length < MAX_BOTS" role="button" class="fw-bold text-primary" @click="addBot">+ Add Bot</a>
      <i v-else class="text-stone-400">Max Bots Added</i>
    </div>
    <div class="header-row" v-if="bots.length">
      <span>Name</span>
      <span>Picking Strategy</span>
    </div>
    <div class="bot-rows">
      <div v-for="(bot, idx) in bots" :key="idx" class="bot-row">
        <div class="bot-name">
          <VInputWrapper icon="user-robot" prefix="sr" small>
            <input
              :value="bot.nickname"
              @input="updateBot(idx, { nickname: ($event.target as HTMLInputElement).value })"
              placeholder="Name"
              class="form-control dark borderless"
              :class="{ 'is-invalid': invalidNicknames[idx] }"
            />
          </VInputWrapper>
        </div>
        <div class="bot-strategy">
          <VInputWrapper small icon="check">
            <select
              :value="bot.pickStyle"
              @change="updateBot(idx, { pickStyle: Number(($event.target as HTMLSelectElement).value) })"
              class="dark borderless"
            >
              <option v-for="o in BOT_PICK_STYLE_OPTIONS" :key="o.text" :value="o.value">
                {{ o.text }}
              </option>
            </select>
          </VInputWrapper>
        </div>
        <div class="bot-delete">
          <a role="button" @click="removeBot(idx)"><VIcon icon="delete" prefix="sr" class="text-stone-400 fs-5" /></a>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.bot-container {
  border-radius: 15px;
  background-color: map-get($custom-colors, 'stone-900');
  overflow: hidden;
}

.bot-container .title-bar {
  padding: 10px 15px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.bot-container .title-bar > * {
  display: block;
}

.bot-container .title-bar > button {
  padding-top: 3px;
  padding-bottom: 3px;
}

.bot-container .header-row {
  background-color: rgba(map-get($custom-colors, 'stone-800'), 0.7);
  padding: 5px 15px;
  text-transform: uppercase;
  font-size: 10px;
  color: map-get($custom-colors, 'stone-400');
  display: flex;
}

.bot-container .bot-row {
  background-color: rgba(map-get($custom-colors, 'stone-700'), 0.7);
  padding: 5px 15px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid map-get($custom-colors, 'stone-600');
  border-bottom: 1px solid map-get($custom-colors, 'stone-800');
}

.bot-container .bot-row:first-child {
  border-top: none;
}

.bot-container .bot-row:last-child {
  border-bottom: none;
}

.bot-container .bot-row .bot-name,
.bot-container .header-row > span:first-child {
  width: 50%;
}

.bot-container .bot-row .bot-strategy,
.bot-container .header-row > span:last-child {
  width: 35%;
}

.bot-container .header-row > span:last-child {
  margin-left: 15px;
}
</style>
