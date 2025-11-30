<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import VIcon from '@/components/VIcon.vue'
import AdminLobbyService from '@/services/admin/AdminLobbyService'
import VInputWrapper from '@/components/VInputWrapper.vue'
import { isYesterday, differenceInDays, compareAsc } from 'date-fns'

//#region data
const lobbySummaries = ref<LobbySummary[]>([])
const isLoading = ref(false)
const filters = reactive<GetAllLobbiesRequest>({
  dateFrom: undefined
})

function ago(date: Date) {
  const now = new Date()

  if (isYesterday(date)) return 'Yesterday'

  const diffInDays = differenceInDays(now, date)
  if (diffInDays < 7) return `${diffInDays}d ago`
  if (diffInDays < 30) return `${Math.floor(diffInDays / 7)}w ago`
  if (diffInDays < 365) return `${Math.floor(diffInDays / 30)}m ago`
  return `${Math.floor(diffInDays / 365)}y ago`
}
//#endregion

//#region methods
async function getLobbySummaries() {
  lobbySummaries.value = (await AdminLobbyService.getAll(filters)).sort((a, b) => compareAsc(b.created, a.created))
}
//#endregion

//#region hooks
onMounted(async () => {
  try {
    isLoading.value = true
    await getLobbySummaries()
  } catch (e) {
    console.error('Error loading lobbies:', e)
  } finally {
    isLoading.value = false
  }
})
//#endregion
</script>

<template>
  <HeaderLayout title="Lobbies" :show-save="false">
    <div class="p-3">
      <div class="d-flex justify-content-between mb-3">
        <div class="me-2">
          <label class="d-block form-label">Date From</label>
          <VInputWrapper>
            <input id="dateFrom" type="date" v-model="filters.dateFrom" class="form-control dark" />
          </VInputWrapper>
        </div>
        <div style="align-self:last baseline">
          <button @click="getLobbySummaries" class="btn btn-primary">
            <VIcon icon="filter" prefix="sr" />
          </button>
        </div>
      </div>

      <div class="table-container w-100">
        <table class="dp-table w-100">
          <tbody>
            <tr v-for="(lobby, idx) in lobbySummaries" :key="lobby.id">
              <td class="pe-3 text-stone-500">
                {{ idx+1  }}
              </td>
              <td>
                <span>
                  <router-link :to="`/lobby/${lobby.joinCode}`" class="d-block text-uppercase fw-bold ls-6">{{
                    lobby.joinCode }}</router-link>
                  <span class="d-block">{{ lobby.isActive ? 'Live' : ago(lobby.created) }}</span>
                </span>
              </td>
              <td class="text-center">
                <div class="d-block">
                  <VIcon prefix="sr" icon="users-alt" class="me-1" />
                  {{ lobby.memberCount }} Members
                </div>
                <div class="d-flex mx-n2 justify-content-center">
                  <div v-if="lobby.memberCount - lobby.botCount - lobby.guestCount" class="mx-2">
                    <VIcon prefix="sr" icon="user-check" class="me-1 text-stone-500" />{{
                      lobby.memberCount - lobby.botCount - lobby.guestCount }}
                  </div>
                  <div v-if="lobby.guestCount" class="mx-2">
                    <VIcon prefix="rr" icon="user" class="me-1 text-stone-500" />{{ lobby.guestCount }}
                  </div>
                  <div v-if="lobby.botCount" class="mx-2">
                    <VIcon prefix="sr" icon="robot" class="me-1 text-stone-500" />{{ lobby.botCount }}
                  </div>
                </div>
              </td>
              <td class="text-end">
                <span class="d-block">
                  <VIcon prefix="sr" icon="hockey-puck" class="me-1 text-stone-500" /> {{ lobby.gameCount }} Games
                </span>
                <span class="d-block">
                  <VIcon prefix="sr" icon="beer" class="me-1 text-stone-500" /> {{ lobby.drinksGiven }} Drinks
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </HeaderLayout>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.table-container {
  overflow-x: auto;
}

.dp-table td,
.dp-table th {
  white-space: nowrap;
}

.dp-table td {
  border-top: 1px solid map-get($custom-colors, 'stone-800');
  padding: 10px 0;
}

.avatar {
  width: 40px;
  height: 40px;
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  border-radius: 50%;
}

.status-badge {
  padding: 0px 5px;
  border-radius: 5px;
  font-weight: bold;
  display: inline-flex;
  font-size: 12px;
  text-transform: capitalize;
  align-items: center;
  margin-right: 4px;
}

.status-badge::before {
  content: '';
  display: block;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  margin-right: 3px;
}

.status-badge.status-active {
  background-color: rgb(221, 250, 221);
  color: rgb(34, 104, 34);
}

.status-badge.status-active::before {
  background-color: rgb(34, 104, 34);
}

.status-badge.status-admin {
  background-color: rgb(205, 226, 253);
  color: rgb(34, 78, 201);
}

.status-badge.status-admin::before {
  background-color: rgb(34, 78, 201);
}

.status-badge.status-inactive {
  background-color: rgb(182, 186, 190);
  color: rgb(71, 71, 73);
}

.status-badge.status-inactive::before {
  background-color: rgb(71, 71, 73);
}

.status-badge.status-guest {
  background-color: rgb(255, 255, 255);
  color: rgb(85, 85, 85);
}

.status-badge.status-guest::before {
  background-color: rgb(85, 85, 85);
}
</style>
