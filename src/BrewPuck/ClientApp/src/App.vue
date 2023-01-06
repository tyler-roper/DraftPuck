<template>
    <div id="app" style="height: 100%;">
        <router-view></router-view>
    </div>
</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';
    import UserService from '@/services/UserService';
    import { mapMutations } from 'vuex';

    @Component({
        methods: { ...mapMutations('lobby', ['setCurrentUserId'])}
    })
    export default class App extends Vue {
        userId: string | null = localStorage.getItem('userId');

        async created() {
            let isValidUser = false;
            if (this.userId) {
                try {
                    await UserService.getUserById(this.userId);
                    isValidUser = true;
                    //console.log(`User validated. (${this.userId})`)
                } catch {
                    //console.log(`User invalid. (${this.userId})`)
                }
            }

            if (!isValidUser) {
                //console.log(`Creating new user...`)
                const user = await UserService.createUser();
                localStorage.setItem('userId', user.id);
                this.userId = user.id;
                //console.log(`User created. (${this.userId})`)
            }

            this.setCurrentUserId(this.userId);
        }
    }
</script>

<style scoped>
    #app {
        height: 100%;
    }
</style>