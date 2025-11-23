<script setup lang="ts">
import FeedItemType from '@/enums/feedItemType'
import LobbyEventType from '@/enums/lobbyEventType'
import PlayType from '@/enums/playType'
import type FeedItem from '@/models/feedItem'
import { format, parseISO } from 'date-fns'
import { computed } from 'vue'
import VIcon from '@/components/VIcon.vue'

//props
const props = defineProps<{
  item: FeedItem
}>()

//computed
const itemStyle = computed(() => {
  if (props.item.subType !== PlayType.Goal) return {}

  return {
    backgroundColor: `${props.item.teamColor} !important`,
    color: 'white !important'
  }
})

const itemClass = computed(() => {
  return isLobbyEvent.value ? `item-type-subtype-${props.item.subType} lobby-event` : `item-type-${props.item.subType.toString().toLowerCase()}`
})

const itemIcon = computed(() => {
  if (!isLobbyEvent.value) {
    return { prefix: "", icon: "" };
  }

  const icons: Record<
    number,
    { prefix: string; icon: string }
  > = {
    [LobbyEventType.UserJoined]: { prefix: "sr", icon: "user-add" },
    [LobbyEventType.UserRejoined]: { prefix: "sr", icon: "user-add" },
    [LobbyEventType.NewPick]: { prefix: "rr", icon: "badge-check" },
    [LobbyEventType.PickRemoved]: { prefix: "sr", icon: "undo" },
    [LobbyEventType.DrinkAssigned]: { prefix: "sr", icon: "beer" },
    [LobbyEventType.DrinkAwarded]: { prefix: "rr", icon: "beer" },
    [LobbyEventType.GoalChanged]: { prefix: "rr", icon: "shuffle" },
    [LobbyEventType.DrinkRevoked]: { prefix: "rr", icon: "comment-slash" },
    [LobbyEventType.UserNameChanged]: { prefix: "rr", icon: "id-badge" },
    [LobbyEventType.DrinkInvalidated]: { prefix: "rr", icon: "trash" },
    [LobbyEventType.GoalRemoved]: { prefix: "sr", icon: "cross-circle" },
    [LobbyEventType.UserRemoved]: { prefix: "sr", icon: "remove-user" },
    [LobbyEventType.Broadcast]: { prefix: "sr", icon: "megaphone" },
    [LobbyEventType.UserLeft]: { prefix: "sr", icon: "exit" },
    [LobbyEventType.UserPromoted]: { prefix: "sr", icon: "crown" }
  };

  return icons[props.item.subType as number] ?? { prefix: "", icon: "" };
});

const isLobbyEvent = computed(() => props.item.type === FeedItemType.LobbyEvent)

//hooks/methods
function getLogo(img: string) {
  return `/img/logos/${img}`
}

function formatAsTime(date: Date | string) {
  date = typeof date === 'string' ? parseISO(date) : date
  return format(date, 'p')
}
</script>

<template>
  <div class="d-flex align-items-center feed-item" :class="itemClass" :style="itemStyle">
    <div class="image-container py-2 px-2">
      <VIcon v-if="isLobbyEvent" class="feed-item-icon" :prefix="itemIcon.prefix" :icon="itemIcon.icon" />
      <div v-else class="logo-container">
        <img v-for="(image, idx) in item.images" :key="idx" :src="getLogo(image)" />
      </div>
    </div>
    <div class="py-2 px-2 content-container flex-grow-1">
      <div class="d-flex justify-content-between">
        <span class="d-block fw-bold text-uppercase">{{ item.title }}</span>
        <span class="d-block o-75">
          <span>{{ item.subtext }}</span>
          <span v-if="item.subtext" class="mx-2">|</span>
          <span>{{ formatAsTime(item.time) }}</span>
        </span>
      </div>
      <div v-html="item.text"></div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.feed-item {
  background-color: map-get($custom-colors, 'stone-0');
  min-height: 75px;
}

.feed-item:not(.feed-item.item-type-subtype-5) {
  border-bottom: 1px solid rgba(map-get($custom-colors, 'stone-300'), 0.2);
}

.image-container {
  box-sizing: content-box;
  width: 65px;
  display: flex;
  align-items: center;
  justify-content: center;
  align-self: stretch;
}

.logo-container {
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.logo-container>img {
  display: block;
}

.logo-container>img:only-child {
  width: 70px;
  margin: -15px 0;
  height: auto;
}

.logo-container>img:not(:only-child) {
  position: absolute;
  width: 45px;
  height: auto;
  top: 50%;
  left: 50%;
  --offset: 10px;
}

.logo-container>img:not(:only-child):first-child {
  transform: translate(calc(-50% + var(--offset)), calc(-50% + var(--offset)));
}

.logo-container>img:not(:only-child):last-child {
  transform: translate(calc(-50% - var(--offset)), calc(-50% - var(--offset)));
}

.feed-item {
  background-color: map-get($custom-colors, 'stone-100');
}

.content-container {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-self: stretch;
  position: relative;
  z-index: 1;
}

.feed-item.item-type-subtype-0,
.feed-item.item-type-subtype-1,
.feed-item.item-type-subtype-2,
.feed-item.item-type-subtype-3,
.feed-item.item-type-subtype-4,
.feed-item.item-type-subtype-8,
.feed-item.item-type-subtype-10,
.feed-item.item-type-subtype-13 {
  background-color: map-get($custom-colors, 'blue') !important;
  color: white;
}

.feed-item.item-type-subtype-6,
.feed-item.item-type-subtype-7,
.feed-item.item-type-subtype-11,
.feed-item.item-type-subtype-14,
.feed-item.item-type-subtype-15 {
  text-shadow: 0 0 5px black;
  background-color: #ffb100;
  color: white;
}

.feed-item.item-type-subtype-5 {
  text-shadow: 0 0 5px black;
  color: white;
  background-size: 100% 400%;
  background-image: linear-gradient(to top,
      map-get($custom-colors, 'amber-500') 0%,
      map-get($custom-colors, 'amber-400') 25%,
      map-get($custom-colors, 'amber-500') 50%,
      map-get($custom-colors, 'amber-400') 75%,
      map-get($custom-colors, 'amber-500') 100%);
  animation: gradient-shift 7s linear infinite;
  position: relative;
  overflow: hidden;
}

.feed-item.item-type-subtype-5::before {
  content: '';
  display: block;
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-image: url(/img/beer-bg.png);
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center 15%;
  opacity: 0.6;
  z-index: 0;
}

@keyframes gradient-shift {
  0% {
    background-position: 0% 0%;
  }

  100% {
    background-position: 0% 200%;
  }
}

.feed-item-icon {
  font-size: 36px;
}
</style>