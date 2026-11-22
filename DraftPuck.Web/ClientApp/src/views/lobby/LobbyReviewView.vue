<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useRoute } from 'vue-router'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import VIcon from '@/components/VIcon.vue'
import VRecapSummary from '@/components/VRecapSummary.vue'
import VRecapAwards from '@/components/VRecapAwards.vue'
import VRecapStandings from '@/components/VRecapStandings.vue'
import VRecapStats from '@/components/VRecapStats.vue'
import { useRecapShare } from '@/composables/useRecapShare'
import { useToast } from '@/composables/useToast'
import { useUserStore } from '@/stores/user'
import { timeAgo } from '@/helpers/dateHelpers'
import { buildRecapCard, recapStandings, recapTotals } from '@/helpers/recapHelpers'
import LobbyService from '@/services/LobbyService'

/** The post-game page: a lobby's totals, then its awards, standings and stats in tabs, and a shareable recap image. */

type RecapTab = 'awards' | 'standings' | 'stats'

const route = useRoute()
const toast = useToast()
const { currentUser } = storeToRefs(useUserStore())
const { isSharing, share } = useRecapShare()

const joinCode = route.params.joinCode as string
const lobby = ref<Lobby>()
const isLoading = ref(false)
const awards = ref<LobbyAward[]>([])
const isLoadingAwards = ref(true)
const players = ref<RecapPlayer[]>([])

// Awards first; a lobby with none skips that tab.
const tab = ref<RecapTab>('awards')
const tabs = computed<RecapTab[]>(() =>
  isLoadingAwards.value || awards.value.length > 0 ? ['awards', 'standings', 'stats'] : ['standings', 'stats']
)
const activeTab = computed(() => (tabs.value.includes(tab.value) ? tab.value : 'standings'))

const dateLabel = computed(() => (lobby.value ? timeAgo(lobby.value.created, new Date(), 'Tonight') : ''))
const totals = computed(() => lobby.value && recapTotals(lobby.value))
/** The viewer's own member, so the stats open on their night. */
const viewerMemberId = computed(() => lobby.value?.members.find((m) => !m.isBot && m.userId === currentUser.value?.id)?.id)

onMounted(async () => {
  if (!joinCode) return
  isLoading.value = true
  // The awards are extra: the page shows without them if they fail.
  LobbyService.getRecap(joinCode)
    .then((recap) => {
      awards.value = recap.awards
      players.value = recap.players ?? []
    })
    .catch((ex) => console.error('Failed to load the recap:', ex))
    .finally(() => (isLoadingAwards.value = false))
  try {
    lobby.value = await LobbyService.getLobbyByCode(joinCode)
  } catch (ex) {
    console.error(ex)
  } finally {
    isLoading.value = false
  }
})

async function shareRecap() {
  if (!lobby.value || !totals.value) return
  const standings = recapStandings(lobby.value).map((s) => ({ name: s.member.name, given: s.given, taken: s.taken }))
  const card = buildRecapCard(joinCode, dateLabel.value, totals.value, awards.value, standings)
  try {
    if ((await share(card, joinCode)) === 'downloaded') toast.success('Recap image downloaded.', { timeout: 2500 })
  } catch (ex) {
    console.error(ex)
    toast.error("Couldn't create the recap image.")
  }
}
</script>

<template>
  <HeaderLayout :title="`Post-Game`" :show-save="false">
    <div class="p-5" v-if="isLoading">Loading</div>
    <div v-if="!isLoading && lobby && totals">
      <VRecapSummary :join-code="joinCode" :date-label="dateLabel" :totals="totals" />
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
      <VRecapAwards v-if="activeTab === 'awards'" class="px-3 pt-3" :awards="awards" :is-loading="isLoadingAwards" :lobby="lobby" />
      <VRecapStandings v-if="activeTab === 'standings'" class="p-3" :lobby="lobby" />
      <div v-if="activeTab === 'stats'" class="p-3">
        <VRecapStats :lobby="lobby" :players="players" :initial-member-id="viewerMemberId" />
      </div>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Awards | Standings | Stats: a segmented control.
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
