<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import HeaderLayout from '@/views/layouts/HeaderLayout.vue'
import VIcon from '@/components/VIcon.vue'
import VUser from '@/components/VUser.vue'
import AdminUserService from '@/services/admin/AdminUserService'
import VInputWrapper from '@/components/VInputWrapper.vue'

//#region data
const users = ref<User[]>([])
const isLoading = ref(false)
const filters = reactive<GetAllUsersRequest>({
  nickname: '',
  includeGuests: false,
  activeOnly: true
})

onMounted(async () => {
  await getUsers()
})

async function getUsers() {
  try {
    isLoading.value = true
    const queryObject = {
      nickname: filters.nickname?.trim().length ? filters.nickname : undefined,
      includeGuests: filters.includeGuests ? true : undefined,
      activeOnly: filters.activeOnly ? undefined : false
    }
    users.value = await AdminUserService.getAll(queryObject)
  } catch (ex) {
    console.error(ex)
  } finally {
    isLoading.value = false
  }
}

function getBadges(user: User) {
  const badges = []
  if (user.isActive) badges.push('active')
  if (!user.isActive) badges.push('inactive')
  if (user.isAdmin) badges.push('admin')
  if (user.isGuest) badges.push('guest')

  return badges
}

//#endregion
</script>

<template>
  <HeaderLayout title="Users" :show-save="false">
    <div class="p-3">
      <div class="d-flex justify-content-between mb-3">
        <div class="me-2">
          <label class="d-block form-label">Nickname</label>
          <VInputWrapper>
            <input id="nickname" v-model="filters.nickname" class="form-control dark" />
          </VInputWrapper>
        </div>
        <div class="me-2">
          <label class="d-block form-label">Guests</label>
          <VInputWrapper>
            <select class="dark" v-model="filters.includeGuests" id="ddlIncludeGuests" style="width: 75px;">
              <option :value="false">No</option>
              <option :value="true">Yes</option>
            </select>
          </VInputWrapper>
        </div>
        <div class="me-2">
          <label class="d-block form-label">Inactive</label>
          <VInputWrapper>
            <select class="dark" v-model="filters.activeOnly" id="ddlIncludeInactive" style="width: 75px;">
              <option :value="true">No</option>
              <option :value="false">Yes</option>
            </select>
          </VInputWrapper>
        </div>
        <div style="align-self:last baseline">
          <button @click="getUsers" class="btn btn-primary">
            <VIcon icon="filter" prefix="sr" />
          </button>
        </div>
      </div>

      <div class="table-container w-100">
        <table class="dp-table w-100">
          <thead>
            <tr>
              <th>Name</th>
              <th></th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>
                <div class="d-flex align-items-center">
                  <div>
                    <router-link :to="{ path: `/u/${user.nickname}` }">
                      <VUser :user="user" display="avatar" :avatar-size-in-px="40" />
                    </router-link>
                  </div>
                  <div class="ms-3">
                    <router-link :to="{ path: `/u/${user.nickname}` }" class="text-white d-block fw-bold">{{
                      user.nickname }}</router-link>
                  </div>
                </div>
              </td>
              <td>
                <div class="badge-container">
                  <span v-for="(badge, idx) in getBadges(user)" :key="idx" class="status-badge"
                    :class="`status-${badge}`">
                    <span class="d-block">{{ badge }}</span>
                  </span>
                </div>
              </td>
              <td class="text-end">
                <a role="button" class="fs-4 text-stone-500">
                  <VIcon icon="edit" prefix="sr" />
                </a>
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
