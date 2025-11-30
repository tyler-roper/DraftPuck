<script setup lang="ts">
import BotPickStyle from '@/enums/botPickStyle'
import LobbyService from '@/services/LobbyService'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import CountUp from 'vue-countup-v3'
import { differenceInDays, isYesterday } from 'date-fns'

interface LobbyMemberWithStats extends LobbyMember {
  results: LobbyMemberResults
  class: string
}

interface LobbyMemberResults {
  totalDrinksAwarded: number
  totalDrinksGiven: number
  totalDrinksTaken: number
  totalPicksMade: number
}

const route = useRoute()
const lobby = ref<Lobby>()
const isLoading = ref(false)
const joinCode = ref(route.params.joinCode as string)
const members = ref<LobbyMemberWithStats[]>([])
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)

const totalDrinks = computed(() => lobby.value?.members.flatMap(m => m.picks.filter(p => p.isActive).flatMap(p => p.drinks).filter(d => !!d.recipientLobbyMemberId)).length ?? 0)
const totalPicks = computed(() => lobby.value?.members.flatMap(m => m.picks.filter(p => p.isActive)).length ?? 0)


onMounted(async () => {
  try {
    if (!joinCode.value) return

    isLoading.value = true
    lobby.value = await LobbyService.getLobbyByCode(joinCode.value)
    setMembers()
    startFadeIns()
  } catch (ex) {
    console.error(ex)
  } finally {
    isLoading.value = false
  }
})

function setMembers() {
  members.value = lobby.value?.members
    .map(member => ({
      ...member,
      results: {
        totalDrinksAwarded: member.picks.filter(pick => pick.isActive).reduce((result, pick) => result + pick.drinks.length, 0),
        totalDrinksGiven: member.picks.filter(pick => pick.isActive).reduce((result, pick) => result + pick.drinks.filter(d => !!d.recipientLobbyMemberId).length, 0),
        totalDrinksTaken: lobby.value?.members.flatMap(m => m.picks.flatMap(p => p.drinks)).filter(drink => drink.recipientLobbyMemberId === member.id).length,
        totalPicksMade: member.picks.filter(pick => pick.isActive).length
      }
    } as LobbyMemberWithStats))
    .sort((b, a) => a.results.totalDrinksGiven - b.results.totalDrinksGiven)
    ?? []
}

function startFadeIns() {
  const delay = 2000 / members.value.length
  for (let i = 0; i < members.value.length; i++)
    setTimeout(() => members.value[i].class = 'fade-in', 250 + (delay * i))
}

function getAvatar(member: LobbyMemberWithStats) {
  return member.isGuest !== false && member.avatarPath ? {} : { 'background-image': `url(${member.avatarPath})` }
}

function getBanner(member: LobbyMemberWithStats) {
  return !member.isGuest && member.banner?.imagePath
    ? { 'background-image': `url(${member.banner.imagePath})` }
    : {}
}

function getIconClass(member: LobbyMemberWithStats) {
  return {
    'text-stone-0': currentUser.value?.id === member.userId,
    'text-stone-700': member.isBot,
    'text-danger': currentUser.value?.id !== member.userId && !member.isBot,
    'fi-sr-user': !member.isBot && lobby.value?.createdBy !== member.userId,
    'fi-sr-user-crown': lobby.value?.createdBy === member.userId,
    'fi-sr-user-robot': member.isBot
  }
}

function getTitle(member: LobbyMemberWithStats) {
  if (member.isBot === true) return `Strategy: ${BotPickStyle[member.botPickStyle]}`
  if (member.isGuest === true) {
    if (currentUser.value?.id === member.userId) return 'You (Guest)'
    else return 'Guest'
  }
  return member.title?.text
}

function ago(date: Date) {
  const now = new Date()

  if (isYesterday(date)) return 'Yesterday'

  const diffInDays = differenceInDays(now, date)
  if (diffInDays < 7) return `${diffInDays}d ago`
  if (diffInDays < 30) return `${Math.floor(diffInDays / 7)}w ago`
  if (diffInDays < 365) return `${Math.floor(diffInDays / 30)}m ago`
  return `${Math.floor(diffInDays / 365)}y ago`
}
</script>

<template>
  <HeaderLayout :title="`Post-Game`" :show-save="false">
    <div class="p-5" v-if="isLoading">Loading</div>
    <div v-if="!isLoading && lobby">
      <div>
        <div class="d-flex justify-content-between bg-stone-900 px-3 py-2 bg-gradient">
          <div>
            <span class="d-block fs-9 fw-bold text-uppercase mb-n1 text-stone-400">Lobby</span>
            <span class="d-block fs-5 fw-bold ls-5">{{ joinCode }}</span>
          </div>
          <div>
            <span class="d-block fs-9 fw-bold text-uppercase mb-n1 text-stone-400 text-end">Date</span>
            <span class="d-block fs-5 fw-bold text-end">{{ ago(lobby.created) }}</span>
          </div>
        </div>
        <div class="d-flex justify-content-between pt-3 bg-stone-900 px-3 pb-2">
          <div class="text-center">
            <span class="d-block fs-7 fw-bold mb-n2 text-uppercase text-primary">Games</span>
            <span class="d-block fs-1 fw-bold">
              <CountUp :end-val="lobby.gameIds.length" :duration="3"></CountUp>
            </span>
          </div>
          <div class="text-center">
            <span class="d-block fs-7 fw-bold mb-n2 text-uppercase text-primary">Players</span>
            <span class="d-block fs-1 fw-bold">
              <CountUp :end-val="members.length" :duration="3"></CountUp>
            </span>
          </div>
          <div class="text-center">
            <span class="d-block fs-7 fw-bold mb-n2 text-uppercase text-primary">Picks</span>
            <span class="d-block fs-1 fw-bold">
              <CountUp :end-val="totalPicks" :duration="3"></CountUp>
            </span>
          </div>
          <div class="text-center">
            <span class="d-block fs-7 fw-bold mb-n2 text-uppercase text-primary">Drinks</span>
            <span class="d-block fs-1 fw-bold">
              <CountUp :end-val="totalDrinks" :duration="3"></CountUp>
            </span>
          </div>
        </div>
      </div>
      <div class="p-3">
        <div class="d-flex member align-items-center w-100" v-for="(member, index) in members" :key="member.id"
          :class="member.class">
          <div class="pe-2 fs-3 fw-bold">{{ index + 1 }}</div>
          <div class="d-flex justify-content-between align-items-center flex-grow-1 p-2 member-result"
            :style="getBanner(member)">
            <div>
              <div class="d-flex align-items-center">
                <div class="icon" :style="getAvatar(member)">
                  <i v-if="!member.avatarPath || member.isGuest" class="fi me-2 d-block"
                    :class="getIconClass(member)"></i>
                </div>

                <div class="d-block">
                  <span class="name d-block fw-bold text-start" :class="{ 'is-logged-in': !member.isGuest }">{{
                    member.name
                  }}</span>
                  <span class="title d-inline-block text-stone-400 mt-n1 fs-7"
                    :class="{ 'is-logged-in': !member.isGuest }">{{ getTitle(member) }}</span>
                </div>
              </div>
            </div>
            <div class="stats-container">
              <span class="stat">
                <span class="stat-title">Given</span>
                <span class="stat-value text-primary">{{ member.results.totalDrinksGiven }}</span>
              </span>
              <span class="stat">
                <span class="stat-title">Taken</span>
                <span class="stat-value text-danger">{{ member.results.totalDrinksTaken }}</span>
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.member {
  opacity: 0;
  transform: translateY(40px);
  animation-timing-function: ease-in;
  margin-top: 10px;
}

.member:first-child .member-result {
  box-shadow: 0 0 15px map-get($custom-colors, 'primary');
}

.member:last-child .member-result {
  box-shadow: 0 0 15px map-get($custom-colors, 'danger');
}

.member-result {
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  overflow: hidden;
  border-radius: 10px;
  background-image: linear-gradient(to right, map-get($custom-colors, 'stone-700'), map-get($custom-colors, 'stone-900'))
}

.member.fade-in {
  animation: fade 1s forwards;
}

.icon {
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

.name {
  height: 18px;
}

.name.is-logged-in {
  text-shadow:
    0 0 5px black,
    0 1px black;
  color: map-get($custom-colors, 'stone-0');
  font-size: 18px !important;
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
  align-self: stretch
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
    transform: translateY(0)
  }
}
</style>