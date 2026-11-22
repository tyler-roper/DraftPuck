<script setup lang="ts">
import { timeAgo } from '@/helpers/dateHelpers'
import BotPickStyle from '@/enums/botPickStyle'
import LobbyService from '@/services/LobbyService'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import CountUp from 'vue-countup-v3'
import { awardStyle, buildRecapCard } from '@/helpers/recapHelpers'
import { useRecapShare } from '@/composables/useRecapShare'
import { useToast } from '@/composables/useToast'
import VIcon from '@/components/VIcon.vue'
import VRecapStats from '@/components/VRecapStats.vue'
import { memberNameScale } from '@/helpers/memberNameHelpers'

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
const awards = ref<LobbyAward[]>([])
const isLoadingAwards = ref(true)
const players = ref<RecapPlayer[]>([])
type RecapTab = 'awards' | 'standings' | 'stats'
// Awards first; a lobby with none skips that tab.
const tab = ref<RecapTab>('awards')
const hasAwards = computed(() => isLoadingAwards.value || awards.value.length > 0)
const tabs = computed<RecapTab[]>(() => (hasAwards.value ? ['awards', 'standings', 'stats'] : ['standings', 'stats']))
const activeTab = computed(() => (tabs.value.includes(tab.value) ? tab.value : 'standings'))
/** The viewer's own member, so the stats open on their night. */
const viewerMemberId = computed(() => lobby.value?.members.find((m) => !m.isBot && m.userId === currentUser.value?.id)?.id)
const { isSharing, share } = useRecapShare()
const toast = useToast()
const userStore = useUserStore()
const { currentUser } = storeToRefs(userStore)

const totalDrinks = computed(
  () =>
    lobby.value?.members.flatMap((m) =>
      m.picks
        .filter((p) => p.isActive)
        .flatMap((p) => p.drinks)
        .filter((d) => !!d.recipientLobbyMemberId)
    ).length ?? 0
)
const totalPicks = computed(() => lobby.value?.members.flatMap((m) => m.picks.filter((p) => p.isActive)).length ?? 0)

onMounted(async () => {
  try {
    if (!joinCode.value) return

    isLoading.value = true
    // The awards are extra: the page shows without them if they fail.
    LobbyService.getRecap(joinCode.value)
      .then((recap) => {
        awards.value = recap.awards
        players.value = recap.players ?? []
      })
      .catch((ex) => console.error('Failed to load the recap:', ex))
      .finally(() => (isLoadingAwards.value = false))
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
  members.value =
    lobby.value?.members
      .map(
        (member) =>
          ({
            ...member,
            results: {
              totalDrinksAwarded: member.picks.filter((pick) => pick.isActive).reduce((result, pick) => result + pick.drinks.length, 0),
              totalDrinksGiven: member.picks
                .filter((pick) => pick.isActive)
                .reduce((result, pick) => result + pick.drinks.filter((d) => !!d.recipientLobbyMemberId).length, 0),
              totalDrinksTaken: lobby.value?.members
                .flatMap((m) => m.picks.flatMap((p) => p.drinks))
                .filter((drink) => drink.recipientLobbyMemberId === member.id).length,
              totalPicksMade: member.picks.filter((pick) => pick.isActive).length
            }
          }) as LobbyMemberWithStats
      )
      .sort((b, a) => a.results.totalDrinksGiven - b.results.totalDrinksGiven) ?? []
}

async function shareRecap() {
  if (!lobby.value) return
  const card = buildRecapCard(
    joinCode.value,
    timeAgo(lobby.value.created, new Date(), 'Tonight'),
    { games: lobby.value.gameIds.length, players: members.value.length, picks: totalPicks.value, drinks: totalDrinks.value },
    awards.value,
    members.value.map((m) => ({ name: m.name, given: m.results.totalDrinksGiven, taken: m.results.totalDrinksTaken ?? 0 }))
  )
  try {
    if ((await share(card, joinCode.value)) === 'downloaded') toast.success('Recap image downloaded.', { timeout: 2500 })
  } catch (ex) {
    console.error(ex)
    toast.error("Couldn't create the recap image.")
  }
}

/** The avatars beside an award's winner: theirs, and their rival's for a rivalry. */
function awardMembers(award: LobbyAward) {
  return [award.memberId, award.otherMemberId]
    .map((id) => lobby.value?.members.find((m) => m.id === id))
    .filter((m): m is LobbyMember => m !== undefined)
    .map((m) => {
      const hasPicture = !!m.avatarPath && !m.isGuest
      return {
        id: m.id,
        style: hasPicture ? { backgroundImage: `url(${m.avatarPath})` } : {},
        initial: hasPicture ? '' : m.name.charAt(0).toUpperCase()
      }
    })
}

function startFadeIns() {
  const delay = 2000 / members.value.length
  for (let i = 0; i < members.value.length; i++) setTimeout(() => (members.value[i].class = 'fade-in'), 250 + delay * i)
}

function getAvatar(member: LobbyMemberWithStats) {
  return member.isGuest !== false && member.avatarPath ? {} : { 'background-image': `url(${member.avatarPath})` }
}

function getBanner(member: LobbyMemberWithStats) {
  return !member.isGuest && member.banner?.imagePath ? { 'background-image': `url(${member.banner.imagePath})` } : {}
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
            <span class="d-block fs-5 fw-bold text-end">{{ timeAgo(lobby.created, new Date(), 'Tonight') }}</span>
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
      <div class="px-3 pt-3">
        <button type="button" class="btn btn-primary fw-bold w-100 py-2" :disabled="isSharing || isLoadingAwards" @click="shareRecap">
          <VIcon icon="share" prefix="sr" class="me-2" />{{ isSharing ? 'Creating image…' : 'Share results' }}
        </button>
      </div>
      <div class="tabs mx-3 mt-3" role="tablist">
        <button
          v-for="t in tabs"
          :key="t"
          type="button"
          role="tab"
          class="tab"
          :class="{ active: activeTab === t }"
          :aria-selected="activeTab === t"
          @click="tab = t"
        >
          {{ t }}
        </button>
      </div>
      <div v-if="activeTab === 'awards'" class="px-3 pt-3">
        <!-- Placeholders hold the awards' place while they load, so the page doesn't jump when they arrive. -->
        <template v-if="isLoadingAwards">
          <div v-for="n in 3" :key="n" class="award award-placeholder mb-2" aria-hidden="true"></div>
        </template>
        <!-- The top award keeps shining; each icon moves in its own way (see awardStyle). -->
        <div
          v-for="(award, index) in awards"
          :key="award.key"
          class="award award-in d-flex align-items-center mb-2 py-2 pe-2 ps-3"
          :class="{ top: index === 0 }"
          :style="{ '--award-color': awardStyle(award.key).color, '--delay': `${index * 90}ms` }"
        >
          <VIcon :icon="awardStyle(award.key).icon" prefix="sr" class="award-icon fs-3 me-3" :class="`motion-${awardStyle(award.key).motion}`" />
          <div class="flex-grow-1">
            <span class="d-block fs-9 fw-bold text-uppercase ls-2 award-title">{{ award.title }}</span>
            <span class="winner fw-bold">
              <span class="winner-avatars">
                <span v-for="m in awardMembers(award)" :key="m.id" class="winner-avatar" :style="m.style">{{ m.initial }}</span>
              </span>
              {{ award.memberName }}
            </span>
            <span class="d-block fs-7 text-stone-300">{{ award.detail }}</span>
          </div>
        </div>
      </div>
      <div v-if="activeTab === 'stats' && lobby" class="p-3">
        <VRecapStats :lobby="lobby" :players="players" :initial-member-id="viewerMemberId" />
      </div>
      <div v-if="activeTab === 'standings'" class="p-3">
        <div class="d-flex member align-items-center w-100" v-for="(member, index) in members" :key="member.id" :class="member.class">
          <div class="pe-2 fs-3 fw-bold">{{ index + 1 }}</div>
          <div class="d-flex justify-content-between align-items-center flex-grow-1 p-2 member-result" :style="getBanner(member)">
            <div>
              <div class="d-flex align-items-center">
                <div class="icon" :style="getAvatar(member)">
                  <i v-if="!member.avatarPath || member.isGuest" class="fi me-2 d-block" :class="getIconClass(member)"></i>
                </div>

                <div class="d-block name-block">
                  <span
                    class="name fw-bold text-start"
                    :class="{ 'is-logged-in': !member.isGuest }"
                    :style="{ '--name-scale': memberNameScale(member.name) }"
                    >{{ member.name }}</span
                  >
                  <span class="title d-inline-block text-stone-400 fs-7" :class="{ 'is-logged-in': !member.isGuest }">{{ getTitle(member) }}</span>
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

// Each award in its colour: a gradient tint, a tinted border and a glowing icon.
.award {
  position: relative;
  overflow: hidden;
  border-radius: 10px;
  border: 1px solid color-mix(in srgb, var(--award-color) 35%, map-get($custom-colors, 'stone-700'));
  border-left: 4px solid var(--award-color);
  background: linear-gradient(
    110deg,
    color-mix(in srgb, var(--award-color) 20%, map-get($custom-colors, 'stone-850')),
    map-get($custom-colors, 'stone-850') 70%
  );
}

.award-icon,
.award-title {
  color: var(--award-color);
}

.award-icon {
  display: inline-block;
  filter: drop-shadow(0 0 6px color-mix(in srgb, var(--award-color) 70%, transparent));
}

// The same height as an award card, with a slow shimmer.
.award-placeholder {
  height: 72px;
  --award-color: #{map-get($custom-colors, 'stone-700')};
  background: linear-gradient(
    90deg,
    map-get($custom-colors, 'stone-850') 25%,
    map-get($custom-colors, 'stone-800') 50%,
    map-get($custom-colors, 'stone-850') 75%
  );
  background-size: 200% 100%;
  animation: award-shimmer 1.4s ease-in-out infinite;
}

// Cards slide in one after another, each with a shine sweeping across.
.award-in {
  animation: award-in 0.4s ease-out both;
  animation-delay: var(--delay);
}

.award-in::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: linear-gradient(100deg, transparent 30%, rgba(255, 255, 255, 0.22) 50%, transparent 70%);
  transform: translateX(-120%);
  animation: award-shine 0.9s ease-out both;
  animation-delay: calc(var(--delay) + 0.25s);
}

// The top award keeps shining, and glows.
.award.top {
  animation:
    award-in 0.4s ease-out both,
    award-glow 2.6s ease-in-out 0.6s infinite;
}

.award.top::after {
  animation: award-shine-repeat 4s ease-in-out infinite;
  animation-delay: calc(var(--delay) + 0.25s);
}

.motion-bob {
  animation: icon-bob 2.2s ease-in-out infinite;
}

.motion-shiver {
  animation: icon-shiver 0.35s linear infinite;
}

.motion-beat {
  animation: icon-beat 1.1s ease-in-out infinite;
}

.motion-jab {
  animation: icon-jab 1.6s ease-in-out infinite;
}

.motion-clash {
  animation: icon-clash 1.4s ease-in-out infinite;
}

.motion-swing {
  transform-origin: top center;
  animation: icon-swing 2s ease-in-out infinite;
}

.motion-wobble {
  animation: icon-wobble 1.8s ease-in-out infinite;
}

.motion-tick {
  animation: icon-tick 1s steps(2) infinite;
}

.motion-twinkle {
  animation: icon-twinkle 2.4s ease-in-out infinite;
}

.motion-bounce {
  animation: icon-bounce 1.3s ease-in-out infinite;
}

@keyframes award-shimmer {
  from {
    background-position: 200% 0;
  }
  to {
    background-position: -200% 0;
  }
}

@keyframes award-in {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}

@keyframes award-shine {
  to {
    transform: translateX(120%);
  }
}

@keyframes award-shine-repeat {
  0% {
    transform: translateX(-120%);
  }
  30%,
  100% {
    transform: translateX(120%);
  }
}

@keyframes award-glow {
  0%,
  100% {
    box-shadow: 0 0 0 transparent;
  }
  50% {
    box-shadow: 0 0 18px color-mix(in srgb, var(--award-color) 45%, transparent);
  }
}

@keyframes icon-bob {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-3px);
  }
}

@keyframes icon-shiver {
  0%,
  100% {
    transform: translateX(0);
  }
  25% {
    transform: translateX(-1px) rotate(-4deg);
  }
  75% {
    transform: translateX(1px) rotate(4deg);
  }
}

@keyframes icon-beat {
  0%,
  100% {
    transform: scale(1);
  }
  15% {
    transform: scale(1.18);
  }
  30% {
    transform: scale(1);
  }
  45% {
    transform: scale(1.12);
  }
}

@keyframes icon-jab {
  0%,
  60%,
  100% {
    transform: translateX(0);
  }
  70% {
    transform: translateX(5px) scale(1.1);
  }
}

@keyframes icon-clash {
  0%,
  100% {
    transform: rotate(0);
  }
  40% {
    transform: rotate(-20deg);
  }
  55% {
    transform: rotate(10deg) scale(1.1);
  }
}

@keyframes icon-swing {
  0%,
  100% {
    transform: rotate(-10deg);
  }
  50% {
    transform: rotate(10deg);
  }
}

@keyframes icon-wobble {
  0%,
  100% {
    transform: rotate(0);
  }
  25% {
    transform: rotate(-10deg);
  }
  75% {
    transform: rotate(10deg);
  }
}

@keyframes icon-tick {
  from {
    transform: rotate(-6deg);
  }
  to {
    transform: rotate(6deg);
  }
}

@keyframes icon-twinkle {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(0.9);
  }
}

@keyframes icon-bounce {
  0%,
  100% {
    transform: translateY(0);
  }
  30% {
    transform: translateY(-4px);
  }
  50% {
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .award-placeholder,
  .award-in,
  .award-in::after,
  .award.top,
  .award-icon {
    animation: none !important;
  }
}

// The winner: a small avatar (two, overlapping, for a rivalry) beside the name.
.winner {
  display: flex;
  align-items: center;
  gap: 7px;
}

.winner-avatars {
  display: flex;
  flex: none;
}

.winner-avatar {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  line-height: 1;
  font-size: 10px;
  font-weight: 800;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'stone-300') center / cover no-repeat;
  box-shadow: 0 0 0 2px map-get($custom-colors, 'stone-850');
}

.winner-avatar + .winner-avatar {
  margin-left: -7px;
}

// Awards | Standings: a segmented control.
.tabs {
  display: flex;
  padding: 4px;
  gap: 4px;
  border-radius: 10px;
  background: map-get($custom-colors, 'stone-850');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.tab {
  flex: 1;
  padding: 7px 0;
  border: 0;
  border-radius: 7px;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: map-get($custom-colors, 'stone-400');
  background: transparent;
}

.tab.active {
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'primary');
}
</style>
