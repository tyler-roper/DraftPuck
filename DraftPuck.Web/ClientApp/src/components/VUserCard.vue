<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import VIcon from '@/components/VIcon.vue'

//data
const userStore = useUserStore()
const { currentUser: user, isAdmin, isLoggedIn } = storeToRefs(userStore)
</script>

<template>
  <div class="user" v-if="isLoggedIn">
    <div class="settings">
      <a role="button"><VIcon class="fs-6" icon="user-pen" prefix="ss"></VIcon></a>
    </div>
    <div class="logout">
      <a role="button" class="text-stone-500"><VIcon class="fs-6" icon="exit" prefix="br"></VIcon></a>
    </div>
    <div class="avatar-container">
      <div class="avatar">
        <i class="no-avatar fi fi-sr-user"></i>
      </div>
      <div class="badge">
        <i class="fi fi-ss-badge-check text-light-blue" :class="{ 'text-primary': isAdmin }"></i>
      </div>
    </div>
    <div class="name mt-2">{{ user?.nickname }}</div>
    <div class="email text-stone-400">{{ user?.email }}</div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

.user {
  position: relative;
  width: 100%;
  padding: 30px;
  padding-bottom: 20px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  flex-direction: column;
  background: linear-gradient(
    to bottom,
    map-get($custom-colors, 'stone-1000'),
    map-get($custom-colors, 'stone-900') 73px,
    map-get($custom-colors, 'stone-700') 74px,
    map-get($custom-colors, 'stone-800') 74px,
    map-get($custom-colors, 'stone-900') 100%
  );
  box-shadow: 0 0 15px rgba(0,0,0,0.4);
}

.user > .settings,
.user > .logout {
  position: absolute;
  top: 13px;
}

.user > .settings {
  left: 16px;
  color: map-get($custom-colors, 'primary');
}

.user > .logout {
  right: 16px;
}

.user > .avatar-container {
  position: relative;
  width: 80px;
  height: 80px;
}

.user > .avatar-container > .badge {
  position: absolute;
  top: 60%;
  left: 60%;
  font-size: 20px;
  text-shadow: 0 0 5px rgba(0, 0, 0, 0.5);
}

.user > .avatar-container > .avatar {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background-color: map-get($custom-colors, 'stone-600');
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border: 4px solid map-get($custom-colors, 'stone-800');
}

.user > .avatar-container > .avatar > .no-avatar {
  display: block;
  font-size: 55px;
  color: map-get($custom-colors, 'stone-400');
  transform: translateY(20px);
}

.user > .name {
  font-weight: bold;
  font-size: 20px;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-top: 5px;
}
</style>
