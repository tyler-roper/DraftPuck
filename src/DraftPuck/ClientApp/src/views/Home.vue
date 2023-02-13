<template>
    <div class="d-flex align-items-center justify-content-center bg-net" style="height: 100%; width: 100%;">
        <div class="shadow overflow-hidden p-5" style="border-radius: 20px; background-color: rgba(0,0,0,0.5)">
            <div style="width: 306px;">
                <div class="mt-n2 text-center">
                    <span class="fs-1 text-stone-100" style="font-family: 'Rubik Mono One';">DraftPuck</span>
                    <span class="d-block text-uppercase text-stone-300 fs-6 mt-n2" style="letter-spacing: 1px;">A live hockey drinking game</span>
                </div>
                <template v-if="!showLobbySettings">
                    <div class="d-flex mt-3 mx-n2">
                        <div class="mx-2" style="width: 30%">
                            <span>Lobby Code</span>
                            <b-form-input v-model="code" placeholder="Code" class="font-weight-bold code-input px-3 py-4 text-stone-900" style="width: 100%" maxlength="4"></b-form-input>
                        </div>
                        <div class="mx-2 flex-grow-1">
                            <span>Name</span>
                            <b-form-input ref="nameInput" v-model="name" placeholder="Wayne Gretzky" class="px-3 py-4" style="width: 100%" maxlength="20"></b-form-input>
                        </div>
                    </div>
                    <div class="mt-3 d-flex">
                        <button @click="joinLobby" class="d-block btn btn-primary w-100 font-weight-bold py-3 text-uppercase" :disabled="isLoading">
                            <span v-if="!isJoiningLobby">Join Lobby</span>
                            <b-spinner v-if="isJoiningLobby" class="m-n2" style="height: 30px; width: 30px;"></b-spinner>
                        </button>
                    </div>

                    <div class="d-flex align-items-center my-4">
                        <div class="bg-stone-600 flex-grow-1" style="height: 1px;"></div>
                        <span class="d-block px-4 text-stone-100 font-weight-bold">OR</span>
                        <div class="bg-stone-600  flex-grow-1" style="height: 1px;"></div>
                    </div>

                    <div class="text-center">
                        <button @click="startCreateLobby" class="d-block btn bg-stone-700 w-100 font-weight-bold py-3 text-uppercase" :disabled="isLoading">
                            <span>Create Lobby</span>
                        </button>
                    </div>
                </template>

                <template v-if="showLobbySettings">
                    <div class="mt-3 py-1 w-100 text-center fs-4 text-uppercase" style="border-top: 1px solid rgb(100,100,100); border-bottom: 1px solid rgb(100,100,100);">
                        New Lobby
                    </div>

                    <div class="mt-4 d-flex mx-n2">
                        <div class="mx-2" style="width: 60%;">
                            <span>Your Name</span>
                            <b-form-input v-model="name" placeholder="Wayne Gretzky" class="px-3 py-4" maxlength="20" style="width: 100%;"></b-form-input>
                        </div>

                        <div class="mx-2" style="width: 40%;">
                            <span>Picks Per Team</span>
                            <b-form-input type="number" v-model="settings.picksPerTeam" class="px-3 py-4" style="width: 100%;"></b-form-input>
                        </div>
                    </div>



                    <div class="mt-3">
                        <span>Bots:</span>
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
                        <button @click="createLobby" class="d-block btn bg-primary w-100 font-weight-bold py-3 text-uppercase" :disabled="isLoading">
                            <span v-if="!isCreatingLobby">Create Lobby</span>
                            <b-spinner v-if="isCreatingLobby" class="m-n2" style="height: 30px; width: 30px;"></b-spinner>
                        </button>
                    </div>

                    <div class="text-center mt-5">
                        <a role="button" @click="showLobbySettings = false" :disabled="isLoading" class="font-weight-bold">&lt; Back</a>
                    </div>
                </template>
                <div class="mt-5 d-flex justify-content-between" >
                    <div>
                        <a role="button" @click="showHelp = !showHelp">
                        <i v-if="!showHelp" class="fi fi-rr-caret-right mr-1"></i>
                        <i v-if="showHelp" class="fi fi-rr-caret-down mr-1"></i>
                        How does it work?</a>
                    </div>
                    <div>
                        <span v-if="loadedGames" class="d-block text-stone-300 ">({{ gameCount }} games left today)</span>
                        <span v-if="!loadedGames" class="d-block text-stone-300 ">Fetching games...</span>
                    </div>
                </div>
                <div v-if="showHelp" class="mt-3 p-3 bg-stone-900">
                    <p class="m-0 p-0 lh-2"><strong>DRAFTPUCK</strong> is a drinking game that takes place during live NHL games.<br /><br />
                    The rules are simple: users pick a player from each team. If your player scores, you make someone else drink a beer!<br /><br />
                    Looking for a twist? Add some bots! Bots make their picks based on the "pick style" assigned to them, which can be anything from auto-picking the best player available, to choosing
                    completely at random. If their player scores, a random user in the lobby will be picked to drink!</p>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Ref } from 'vue-property-decorator';
    import LobbyService from '@/services/LobbyService';
    import BotNames from '@/models/botNames';
    import BotPickStyle from '@/enums/botPickStyle';
    import '@/extensions/arrayExtensions';
    import NHL from '@/services/NhlApiService';
    import format from 'date-fns/format';
    import addHours from 'date-fns/addHours';
    import GameStatusCode from '@/models/nhlApi/enums/gameStatusCode';

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

        @Ref('nameInput')
        nameInput!: HTMLInputElement;

        isCreatingLobby = false;
        isJoiningLobby = false;
        get isLoading() { return this.isCreatingLobby || this.isJoiningLobby }
        loadedGames = false;
        gameCount = 0;
        showHelp = false;

        mounted() {
            if (this.code != "" && this.name === null)
                this.$nextTick(() => this.nameInput.focus());
        }

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

        async created() {
            const latestLobby = localStorage.getItem('latestLobby');
            if (latestLobby != null) {
                const latestLobbyParsed: { joinCode: string; name: string } = JSON.parse(latestLobby);
                this.name = latestLobbyParsed.name;
                this.code = latestLobbyParsed.joinCode;
            }

            const schedule = await NHL.getSchedule(format(addHours(new Date(), -10), 'yyyy-MM-dd'));
            this.gameCount = schedule.dates[0].games.filter(g => ![GameStatusCode.Final, GameStatusCode.Final2, GameStatusCode.GameOver, GameStatusCode.Postponed].includes(g.status.statusCode)).length;
            this.loadedGames = true;
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
            if (!this.name || this.name.trim() === "") {
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

            try {
                this.isCreatingLobby = true;

                const lobby = await LobbyService.createLobby({ name: this.name, picksPerTeam: this.settings.picksPerTeam });

                const botPromises = this.settings.bots.map(async b => await LobbyService.joinLobbyByCode(lobby.joinCode, b.name, true, Number(b.pickStyle)));
                await Promise.all(botPromises);

                this.$router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } });
            } catch {
                this.$toast.error("Something went wrong.");
                this.isCreatingLobby = false;
            }
        }

        async joinLobby() {
            if (!this.name || this.name.trim() === "") {
                this.$toast.error("Your name cannot be blank.");
                return;
            }

            if (this.code.length != 4) {
                this.$toast.error("Invalid code.");
                return;
            }

            try {
                this.isJoiningLobby = true;
                const lobby = await LobbyService.getLobbyByCode(this.code);
                const existingMember = lobby.members.find(m => m.name.trim().toLowerCase() === this.name.trim().toLowerCase());
                const userId = localStorage.getItem('userId');

                if (existingMember && userId && existingMember.userId !== userId) {
                    this.$toast.error("Username already taken.");
                    return;
                }

                await LobbyService.joinLobbyByCode(this.code, this.name);
                this.$router.push({ name: 'Lobby', params: { joinCode: lobby.joinCode } });
            } catch {
                this.$toast.error("Lobby not found.");
                this.isJoiningLobby = false;
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

        .code-input::-webkit-input-placeholder { /* WebKit browsers */
            text-transform: none;
            font-weight: normal !important;
            letter-spacing: normal !important;
        }

        .code-input:-moz-placeholder { /* Mozilla Firefox 4 to 18 */
            text-transform: none;
            font-weight: normal !important;
            letter-spacing: normal !important;
        }

        .code-input::-moz-placeholder { /* Mozilla Firefox 19+ */
            text-transform: none;
            font-weight: normal !important;
            letter-spacing: normal !important;
        }

        .code-input:-ms-input-placeholder { /* Internet Explorer 10+ */
            text-transform: none;
            font-weight: normal !important;
            letter-spacing: normal !important;
        }

        .code-input::placeholder { /* Recent browsers */
            text-transform: none;
            font-weight: normal !important;
            letter-spacing: normal !important;
        }
</style>