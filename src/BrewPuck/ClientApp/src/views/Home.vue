<template>
    <div class="d-flex align-items-center justify-content-center bg-net" style="height: 100%; width: 100%;">
        <div class="shadow overflow-hidden p-5" style="border-radius: 20px; background-color: rgba(0,0,0,0.5)">
            <div class="mb-3 mt-n2">
                <span class="fs-1 text-stone-100" style="font-family: 'Rubik Mono One';">BrewPuck</span>
            </div>
            <div>
                <b-form-input v-model="code" placeholder="Code" class="px-3 py-4 text-stone-900" max-length="4"></b-form-input>
            </div>
            <div class="mt-3">
                <b-form-input v-model="name" placeholder="Name" class="px-3 py-4"></b-form-input>
            </div>
            <div class="mt-3 d-flex">
                <button @click="joinLobby" class="d-block btn btn-primary w-100 font-weight-bold py-3 text-uppercase">Join Lobby</button>
            </div>

            <div class="d-flex align-items-center my-4">
                <div class="bg-stone-600 flex-grow-1" style="height: 1px;"></div>
                <span class="d-block px-4 text-stone-100 font-weight-bold">OR</span>
                <div class="bg-stone-600  flex-grow-1" style="height: 1px;"></div>
            </div>

            <div class="text-center">
                <button @click="createLobby" class="d-block btn bg-stone-700 w-100 font-weight-bold py-3 text-uppercase">Create Lobby</button>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';
    import LobbyService from '@/services/LobbyService';

    @Component
    export default class Home extends Vue {
        name = "";
        code = "";

        async createLobby() {
            const lobby = await LobbyService.createLobby("Test Person");
            this.$router.push({ name: 'Lobby', params: { code: lobby.joinCode } });
        }

        async joinLobby() {
            const lobby = await LobbyService.joinLobbyByCode(this.code, this.name);
            this.$router.push({ name: 'Lobby', params: { code: lobby.joinCode } });
        }
    }
</script>

<style scoped>
    .bg-net {
        background-image: linear-gradient(rgba(0,0,0,0.8), rgba(0,0,0,0.8)), url(~@/assets/img/net.jpg);
        background-position: center, -600px -700px;
        background-size: 3500px;
    }
</style>