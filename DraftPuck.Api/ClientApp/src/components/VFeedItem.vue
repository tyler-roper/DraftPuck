<script setup lang="ts">
import FeedItemType from '@/enums/feedItemType'
import LobbyEventType from '@/enums/lobbyEventType'
import PlayType from '@/enums/playType'
import type FeedItem from '@/models/feedItem'
import { format, parseISO } from 'date-fns'
import { computed } from 'vue'

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
  return isLobbyEvent.value ? `item-type-subtype-${props.item.subType}` : `item-type-${props.item.subType.toString().toLowerCase()}`
})

const itemIcon = computed(() => {
  if (!isLobbyEvent.value) return ''

  const icons: { [key: number]: string } = {
    [LobbyEventType.UserJoined]: 'fi-sr-user-add',
    [LobbyEventType.UserRejoined]: 'fi-sr-user-add',
    [LobbyEventType.NewPick]: 'fi-rr-badge-check',
    [LobbyEventType.DrinkAssigned]: 'fi-sr-beer',
    [LobbyEventType.DrinkAwarded]: 'fi-rr-beer',
    [LobbyEventType.GoalChanged]: 'fi-rr-shuffle',
    [LobbyEventType.DrinkRevoked]: 'fi-rr-comment-slash',
    [LobbyEventType.UserNameChanged]: 'fi-rr-id-badge',
    [LobbyEventType.DrinkInvalidated]: 'fi-rr-trash',
    [LobbyEventType.GoalRemoved]: 'fi-sr-cross-circle',
    [LobbyEventType.UserRemoved]: 'fi-sr-remove-user',
    [LobbyEventType.Broadcast]: 'fi-sr-megaphone'
  }

  return icons[props.item.subType as number]
})

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
    <div class="team-icons p-3 ms-n2 flex-shrink-0" style="width: 140px">
      <i v-if="isLobbyEvent" class="d-block fs-2 ps-3 mb-n1 pt-2 fi" :class="itemIcon"></i>
      <img v-for="(image, idx) in item.images" :key="idx" :src="getLogo(image)" />
    </div>
    <div class="flex-grow-1 px-4 py-3 feed-item-content" style="margin-left: -70px">
      <div class="d-flex justify-content-between header">
        <span class="d-block fw-bold text-uppercase header-text">{{ item.title }}</span>
        <span class="d-block timestamps" style="opacity: 0.7">
          <span>{{ item.subtext }}</span>
          <span v-if="item.subtext" class="mx-2">|</span>
          <span>{{ formatAsTime(item.time!) }}</span>
        </span>
      </div>
      <span class="d-block event-text mt-1" v-html="item.text"></span>
    </div>
  </div>
</template>
