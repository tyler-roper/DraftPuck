<template>
    <div class="d-flex align-items-center justify-content-center bg-net" style="height: 100%; width: 100%;">
        <div class="shadow overflow-hidden p-5" style="border-radius: 20px; background-color: rgba(0,0,0,0.5)">
            <div style="width: 300px">
                <div class="mb-3 mt-n2 text-center">
                    <span class="fs-1 text-stone-100" style="font-family: 'Rubik Mono One';">DraftPuck</span>
                </div>
                <template v-if="!showLobbySettings">
                    <div>
                        <b-form-input v-model="code" placeholder="Code" class="font-weight-bold code-input px-3 py-4 text-stone-900" maxlength="4"></b-form-input>
                    </div>
                    <div class="mt-3">
                        <b-form-input v-model="name" placeholder="Name" class="px-3 py-4" maxlength="20"></b-form-input>
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
                        <button @click="startCreateLobby" class="d-block btn bg-stone-700 w-100 font-weight-bold py-3 text-uppercase">Create Lobby</button>
                    </div>
                </template>

                <template v-if="showLobbySettings">
                    <span class="fs-4 text-stone-100 font-weight-bold">New Lobby</span>

                    <div class="mt-3">
                        <span class="text-uppercase font-weight-bold text-stone-300">Your Name</span>
                        <b-form-input v-model="name" placeholder="Wayne Gretzky" class="px-3 py-4" maxlength="20"></b-form-input>
                    </div>

                    <div class="mt-3">
                        <span class="text-uppercase font-weight-bold text-stone-300">Picks Per Team</span>
                        <b-form-input type="number" v-model="settings.picksPerTeam" class="px-3 py-4"></b-form-input>
                    </div>

                    <div class="mt-3">
                        <span class="text-uppercase font-weight-bold text-stone-300">Bots:</span>
                        <span v-if="settings.bots.length === 0">(None)</span>
                        <div class="mt-n2" v-if="settings.bots.length > 0">
                            <div v-for="(bot, idx) in settings.bots" :key="idx" class="my-2 d-flex mx-n2 align-items-center">
                                <div class="px-2" style="width: 45%;">
                                    <b-form-input v-model="bot.name"></b-form-input>
                                </div>
                                <div class="px-2" style="width: 45%;">
                                    <b-form-select v-model="bot.pickStyle" :options="botPickStyles"></b-form-select>
                                </div>
                                <div class="ml-auto pr-2">
                                    <a role="button" @click="removeBot(bot)"><i class="fi fi-rr-x"></i></a>
                                </div>
                            </div>
                        </div>
                        <div>
                            <a role="button" class="d-block text-uppercase font-weight-bold" @click="addBot">Add</a>
                        </div>
                    </div>

                    <div class="text-center mt-5">
                        <button @click="createLobby" class="d-block btn bg-primary w-100 font-weight-bold py-3 text-uppercase">Create Lobby</button>
                    </div>

                    <div class="text-center mt-5">
                        <a role="button" @click="showLobbySettings = false">Back</a>
                    </div>
                </template>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';
    import LobbyService from '@/services/LobbyService';
    import BotNames from '@/models/botNames';
    import BotPickStyle from '@/enums/botPickStyle';
    import '@/extensions/arrayExtensions';

    interface Bot {
        name: string;
        pickStyle: BotPickStyle | null;
    }

    interface LobbySettings {
        picksPerTeam: number | null;
        bots: Array<Bot>;
    }

    @Component
    export default class Home extends Vue {
        name = "";
        code = "";
        showLobbySettings = false;
        
        botPickStyles = [
            { text: "Pick Style", value: null },
            // eslint-disable-next-line @typescript-eslint/no-unused-vars
            ...Object.entries(BotPickStyle).filter(([value, text]: [string, string | number]) => isNaN(Number(text))).map(([value, text]) => ({ text, value }))
        ];

        settings: LobbySettings = {
            picksPerTeam: 1,
            bots: []
        }

        addBot() {
            this.settings.bots.push({ name: this.getRandomBotName(), pickStyle: null });
        }

        removeBot(bot: Bot) {
            this.settings.bots = this.settings.bots.filter(b => b !== bot);
        }

        created() {
            const latestLobby = localStorage.getItem('latestLobby');
            if (latestLobby != null) {
                const latestLobbyParsed: { joinCode: string; name: string } = JSON.parse(latestLobby);
                this.name = latestLobbyParsed.name;
                this.code = latestLobbyParsed.joinCode;
            }
        }

        startCreateLobby() {
            this.showLobbySettings = true;
        }

        getRandomBotName(): string {
            const unusedNames = BotNames.filter(botName => !this.settings.bots.map(bot => bot.name).includes(botName));
            return unusedNames.length
                ? unusedNames.random()
                : `Bot ${this.settings.bots.length}`;
        }

        async createLobby() {
            if (this.name.trim() === "") {
                this.$toast.error("Your name cannot be blank.");
                return;
            }

            if (this.settings.picksPerTeam === null) {
                this.$toast.error("You must set the picks per game. For infinite, choose 0.");
                return;
            }
            
            if (this.settings.bots.some(b => b.name.trim() === "")) {
                this.$toast.error("Bots must have a name.");
                return;
            }

            if (this.settings.bots.some(b => b.name.trim() === this.name.trim())) {
                this.$toast.error("Bot names cannot match your name.");
                return;
            }

            if (this.settings.bots.filter((bot, index) => this.settings.bots.indexOf(bot) != index).length > 0) {
                this.$toast.error("Bot names must be unique.");
                return;
            }

            if (this.settings.bots.some(b => b.pickStyle === null)) {
                this.$toast.error("Bots must have a pick style.");
                return;
            }

            const lobby = await LobbyService.createLobby({ name: this.name, picksPerTeam: this.settings.picksPerTeam });

            const botPromises = this.settings.bots.map(async b => await LobbyService.joinLobbyByCode(lobby.joinCode, b.name, true, Number(b.pickStyle)));
            await Promise.all(botPromises);

            this.$router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } });
        }

        async joinLobby() {
            if (this.name.trim() === "") {
                this.$toast.error("Your name cannot be blank.");
                return;
            }

            if (this.code.length != 4) {
                this.$toast.error("Invalid code.");
                return;
            }

            try {
                const lobby = await LobbyService.joinLobbyByCode(this.code, this.name);
                this.$router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } });
            } catch {
                this.$toast.error("Lobby not found.");
                return;
            }
        }
    }
</script>

<style scoped>
    .bg-net {
        background-image: linear-gradient(rgba(0,0,0,0.8), rgba(0,0,0,0.8)), url(~@/assets/img/net.jpg);
        background-position: center, -600px -700px;
        background-size: 3500px;
    }

    .code-input {
        text-transform: uppercase;
        letter-spacing: 2px;
    }

    ::-webkit-input-placeholder { /* WebKit browsers */
        text-transform: none;
        font-weight: normal !important;
        letter-spacing: normal !important;
    }

    :-moz-placeholder { /* Mozilla Firefox 4 to 18 */
        text-transform: none;
        font-weight: normal !important;
        letter-spacing: normal !important;
    }

    ::-moz-placeholder { /* Mozilla Firefox 19+ */
        text-transform: none;
        font-weight: normal !important;
        letter-spacing: normal !important;
    }

    :-ms-input-placeholder { /* Internet Explorer 10+ */
        text-transform: none;
        font-weight: normal !important;
        letter-spacing: normal !important;
    }

    ::placeholder { /* Recent browsers */
        text-transform: none;
        font-weight: normal !important;
        letter-spacing: normal !important;
    }
</style>