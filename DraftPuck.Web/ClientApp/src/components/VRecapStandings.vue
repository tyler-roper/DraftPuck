<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import { memberAvatar, recapStandings, standingSubtitle } from '@/helpers/recapHelpers'
import { memberNameScale } from '@/helpers/memberNameHelpers'

/** The post-game page's Standings tab: everyone by drinks given, on their banner, fading in one after another. */
const props = defineProps<{
  lobby: Lobby
}>()

const { currentUser } = storeToRefs(useUserStore())
const standings = computed(() => recapStandings(props.lobby))

// The rows fade in over two seconds, top first.
const shownCount = ref(0)
const timers: number[] = []
onMounted(() => {
  const delay = 2000 / standings.value.length
  standings.value.forEach((_, i) => timers.push(window.setTimeout(() => (shownCount.value = i + 1), 250 + delay * i)))
})
onUnmounted(() => timers.forEach((t) => window.clearTimeout(t)))

const banner = (member: LobbyMember) => (!member.isGuest && member.banner?.imagePath ? { 'background-image': `url(${member.banner.imagePath})` } : {})

function iconClass(member: LobbyMember) {
  const isViewer = currentUser.value?.id === member.userId
  const isHost = props.lobby.createdBy === member.userId
  return {
    'text-stone-0': isViewer,
    'text-stone-700': member.isBot,
    'text-danger': !isViewer && !member.isBot,
    'fi-sr-user': !member.isBot && !isHost,
    'fi-sr-user-crown': isHost,
    'fi-sr-user-robot': member.isBot
  }
}
</script>

<template>
  <div>
    <div
      v-for="({ member, given, taken }, index) in standings"
      :key="member.id"
      class="d-flex member align-items-center w-100"
      :class="{ 'fade-in': index < shownCount }"
    >
      <div class="pe-2 fs-3 fw-bold">{{ index + 1 }}</div>
      <div class="d-flex justify-content-between align-items-center flex-grow-1 p-2 member-result" :style="banner(member)">
        <div class="d-flex align-items-center">
          <div class="icon" :style="memberAvatar(member).style">
            <i v-if="!memberAvatar(member).style.backgroundImage" class="fi me-2 d-block" :class="iconClass(member)"></i>
          </div>

          <div class="d-block name-block">
            <span
              class="name fw-bold text-start"
              :class="{ 'is-logged-in': !member.isGuest }"
              :style="{ '--name-scale': memberNameScale(member.name) }"
              >{{ member.name }}</span
            >
            <span class="title d-inline-block text-stone-400 fs-7" :class="{ 'is-logged-in': !member.isGuest }">{{
              standingSubtitle(member, currentUser?.id)
            }}</span>
          </div>
        </div>
        <div class="stats-container">
          <span class="stat">
            <span class="stat-title">Given</span>
            <span class="stat-value text-primary">{{ given }}</span>
          </span>
          <span class="stat">
            <span class="stat-title">Taken</span>
            <span class="stat-value text-danger">{{ taken }}</span>
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.member {
  opacity: 0;
  transform: translateY(40px);
  animation-timing-function: ease-in;
  margin-top: 10px;
}

.member-result {
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  overflow: hidden;
  border-radius: 10px;
  background-image: linear-gradient(to right, map-get($custom-colors, 'stone-700'), map-get($custom-colors, 'stone-900'));
}

.member.fade-in {
  animation: fade 1s forwards;
}

.icon {
  // A fixed circle: a long name shrinks beside it, never the avatar.
  flex: none;
  display: flex;
  width: 50px;
  height: 50px;
  background: map-get($custom-colors, 'stone-0');
  border-radius: 100%;
  align-items: center;
  justify-content: center;
  font-size: 40px;
  overflow: hidden;
  padding-left: 8px;
  padding-top: 25px;
  margin-right: 10px;
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  border: 2px solid map-get($custom-colors, 'stone-0');
}

.name-block {
  // Lets a long name shrink beside the stats rather than push them off the card.
  min-width: 0;
}

// Long names shrink (memberNameScale), wrap to two tight lines, and end with "…" beyond that. No fixed height, so
// the title always sits below the name.
.name {
  font-size: calc(16px * var(--name-scale, 1));
  line-height: 1.1;
  overflow-wrap: anywhere;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.name.is-logged-in {
  text-shadow:
    0 0 5px black,
    0 1px black;
  color: map-get($custom-colors, 'stone-0');
  font-size: calc(18px * var(--name-scale, 1)) !important;
}

.title.is-logged-in {
  text-shadow:
    0 0 5px black,
    0 1px black;
  padding: 0px 2px;
  letter-spacing: 0.5px;
  background: rgba(0, 0, 0, 0.3);
  font-size: 10px !important;
  text-transform: uppercase;
  font-weight: bold;
  text-align: left;
  color: map-get($custom-colors, 'stone-200') !important;
}

.stats-container {
  margin-left: auto;
  display: flex;
  align-items: stretch;
  align-self: stretch;
}

.stat {
  display: flex;
  flex-direction: column;
  justify-content: center;
  width: 50px;
  border-radius: 5px;
  text-align: center;
  font-weight: bold;
  margin-left: 10px;
  padding: 3px;
  background: rgba(0, 0, 0, 0.7);
}

.stat-title {
  display: block;
  text-transform: uppercase;
  font-size: 10px;
}

.stat-value {
  font-size: 20px;
}

@keyframes fade {
  0% {
    opacity: 0;
    transform: translateY(40px);
  }

  100% {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
