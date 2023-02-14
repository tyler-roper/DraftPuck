<template>
    <div class="d-flex overflow-hidden flex-column" style="height: 100%;">
        <template v-if="!isInvalidLobby">

            <div class="bg-black px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center" style="z-index: 10;">
                <router-link to="/" class="banner-logo text-stone-0 text-decoration-none" style="cursor: pointer">
                    <img src="@/assets/img/logo-wide.png" style="height: 65px;" />
                </router-link>

                <div class="ml-auto mr-n2">
                    <a v-if="!isLobbyView" role="button" class="mx-2 badge badge-pill bg-stone-600 py-2 px-3 d-sm-none font-weight-bold text-uppercase text-stone-200" style="text-decoration: none !important;" @click="setView('lobby')">
                        <span>Lobby</span>
                    </a>

                    <a v-if="!isFeedView" role="button" class="mx-2 badge badge-pill bg-stone-600 py-2 px-3 d-sm-none font-weight-bold text-uppercase text-stone-200" style="text-decoration: none !important;" @click="setView('feed')">
                        <span>Feed</span>
                    </a>

                    <a v-if="!isGameView" role="button" class="mx-2 badge badge-pill bg-stone-600 py-2 px-3 d-sm-none font-weight-bold text-uppercase text-stone-200" style="text-decoration: none !important;" @click="setView('game')">
                        <span>Scores</span>
                    </a>
                </div>
            </div>

            <div class="d-flex flex-grow-1 overflow-hidden">
                <template v-if="!isLoading">
                    <FullScoreboard class="full-scoreboard flex-grow-1" :eventBus="eventBus" :class="{ 'hide-mobile': !isGameView }" :games="games" style="overflow: auto;"></FullScoreboard>

                    <div class="feed flex-shrink-0 d-flex flex-column" :class="{ 'hide-mobile': !isFeedView && !isLobbyView }" style="width: 400px;">
                        <LobbyOverview ref="overview" style="min-height: 300px;" :class="{ 'hide-mobile': !isLobbyView }"></LobbyOverview>
                        <Feed class="flex-grow-1" :items="feedItems" :class="{ 'hide-mobile': !isFeedView }"></Feed>
                    </div>
                </template>

                <div v-if="isLoading" style="width: 100%; height: 100%;" class="d-flex align-items-center">
                    <div class="mx-auto d-flex flex-column align-items-center">
                        <b-spinner class="d-block mt-3" style="width: 150px; height: 150px;" color="white"></b-spinner>
                        <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase font-weight-bold">{{loadingMessage}}...</span>
                    </div>
                </div>
            </div>

        </template>

        <template v-if="isInvalidLobby">
            <div style="width: 100%; height: 100%;" class="d-flex align-items-center">
                <div class="mx-auto d-flex flex-column align-items-center">
                    <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase font-weight-bold">Sorry, this lobby is invalid.</span>
                </div>
            </div>
        </template>

        <div v-if="currentDrink" class="drink-animation d-flex align-items-center justify-content-center">
            <span class="text-white text-center">
                <span class="text-uppercase font-weight-bold" style="font-size: 100px;">Drink!</span>
                <span class="d-block fs-5" style="opacity: 0.5">
                    Courtesy Of
                </span>
                <span class="d-block font-weight-bold fs-2 text-uppercase">
                    {{ getSenderNameByLobbyEvent(currentDrink) }}
                </span>
            </span>
            
        </div>

    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop, Ref } from 'vue-property-decorator';
    import addHours from 'date-fns/addHours';
    import addMinutes from 'date-fns/addMinutes';
    import format from 'date-fns/format';
    import LobbyOverview from '@/components/LobbyOverview.vue';
    import FullScoreboard from '@/components/FullScoreboard.vue';
    import Feed from '@/components/Feed.vue';
    import NHL from '@/services/NhlApiService';
    import FeedItem from '@/models/feedItem';
    import * as jsonpatch from 'fast-json-patch';
    import parseISO from 'date-fns/parseISO';
    import EventType from '@/models/nhlApi/enums/eventType';
    import GameStatusCode from '@/models/nhlApi/enums/gameStatusCode';
    import LoadingMessages from '@/models/loadingMessages';
    import LobbyEventType from '@/enums/lobbyEventType';
    import { mapState, mapActions, mapGetters, mapMutations } from 'vuex';
    import VueResizable from 'vue-resizable'
    import '@/extensions/arrayExtensions';
    import * as signalR from "@microsoft/signalr";
    import TeamColorLookup from '@/models/teamColorLookup';

    type View = "feed" | "game" | "lobby";

    @Component({
        components: { LobbyOverview, FullScoreboard, Feed, VueResizable },
        computed: {
            ...mapState('lobby', ['lobby', 'currentUserId', 'lobbyEvents']),
            ...mapGetters('lobby', ['isLobbyAdmin'])
        },
        methods: {
            ...mapActions('lobby', ['getLobby', 'getLobbyEvents']),
            ...mapMutations('lobby', ['addLobbyEvent', 'addPick'])
        }
    })
    export default class LobbyComponent extends Vue {
        //PROPS
        @Prop()
        joinCode!: string;

        //STORE
        lobby!: Lobby;
        lobbyEvents!: Array<LobbyEvent>;
        mappedLobbyEvents: Array<LobbyEvent> = [];
        currentUserId!: string;
        getLobby!: (joinCode: string) => void;
        getLobbyEvents!: (lobbyId: string) => void;
        addLobbyEvent!: (event: LobbyEvent) => void;
        isLobbyAdmin!: boolean;
        addPick!: (pick: LobbyMemberPick) => void;

        //DATA
        sessionStart: Date = new Date();
        games: Array<LiveGame> = [];
        lastLobbyUpdate = new Date(-1);

        selectedDate: string = format(addHours(new Date(), -10), 'yyyy-MM-dd');

        isInvalidLobby = false;
        isLoading = false;

        timers: Array<number> = [];
        eventBus = new Vue();
        connection: HubConnection | null = null;

        view: View = "game";

        pendingDrinks: Array<LobbyEvent> = [];
        currentDrink: LobbyEvent | null = null;

        notificationPermissionsGranted = false;

        @Ref('overview') overview!: LobbyOverview;

        //METHODS
        async created() {
            try {
                this.isLoading = true;
                await this.getLobby(this.joinCode);
                this.lastLobbyUpdate = new Date();

                this.selectedDate = format(addHours(this.lobby.created, -10), 'yyyy-MM-dd');

                const currentLobbyMember = this.lobby.members.find(m => m.userId === this.currentUserId);
                const name = currentLobbyMember?.name ?? null;

                localStorage.setItem('latestLobby', JSON.stringify({ joinCode: this.lobby.joinCode, name }))
                
                if (!currentLobbyMember) {
                    this.$router.push({ name: 'Home' });
                }

                await this.getLobbyEvents(this.lobby.id);
                await this.connectToEventSource();
                await this.setGames();

                this.mappedLobbyEvents = this.lobbyEvents.map(evt => this.replaceTemplatedStrings(evt));
            } catch (e) {
                console.error(e);
                this.isInvalidLobby = true;
                return;
            } finally {
                this.isLoading = false;
            }

            const permission = await Notification.requestPermission();
            this.notificationPermissionsGranted = permission === 'granted';
        }

        setView(view: View) {
            this.view = view;
        }

        async setGames() {
            this.games = [];

            const schedule = await NHL.getSchedule(this.selectedDate);
            if (schedule.dates.length === 0 || schedule.dates[0].games.length === 0) return;

            const gamePromises = schedule.dates[0].games.map(async game => await this.getGameData(game.gamePk));
            this.games = await Promise.all(gamePromises);

            this.games.forEach(game => {
                if (!this.gameIsStale(game))
                    this.timers.push(setTimeout(() => this.pollForUpdates(game), 10000))
            });
        }

        async updateGame(game: LiveGame) {
            const isInProgress = [GameStatusCode.InProgress, GameStatusCode.InProgressCritical].includes(game.gameData.status.statusCode);
            const gameIndex = this.games.findIndex(g => game === g);

            if (!isInProgress) {
                this.$set(this.games, gameIndex, await this.getGameData(game.gamePk));
                return;
            }

            const result = await NHL.getGamePatch(game.gamePk, game.metaData.timeStamp);
            if (Array.isArray(result)) {
                const patchCollections = result as Array<{ diff: Array<PatchOperation> }>;
                patchCollections.forEach(patchCollection => jsonpatch.applyPatch(game, patchCollection.diff as readonly jsonpatch.Operation[]));
            } else {
                this.$set(this.games, gameIndex, await this.getGameData(game.gamePk));
            }
        }

        async getGameData(gamePk: number) {
            return await NHL.getGameData(gamePk);
        }

        async setDate() {
            this.timers.forEach(timer => clearTimeout(timer));
            this.timers = [];
            await this.setGames();
        }

        async pollForUpdates(game: LiveGame) {
            await this.updateGame(game);
            const isInProgress = [GameStatusCode.InProgress, GameStatusCode.InProgressCritical].includes(game.gameData.status.statusCode);

            if (this.gameIsStale(game)) return;

            const interval = isInProgress
                ? 10000
                : 60000;

            this.timers.push(setTimeout(() => this.pollForUpdates(game), interval));
        }

        gameIsStale(game: LiveGame) {
            const isOver = [GameStatusCode.Final, GameStatusCode.GameOver, GameStatusCode.Final2].includes(game.gameData.status.statusCode);
            return isOver && game.gameData.datetime.endDateTime && (game.gameData.datetime.endDateTime <= addMinutes(new Date(), -10));
        }

        async connectToEventSource() {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl('/hub', signalR.HttpTransportType.ServerSentEvents)
                .configureLogging(signalR.LogLevel.Error)
                .withAutomaticReconnect()
                .build();

            connection.on("LobbyEvent", (lobbyEvent: LobbyEvent) => {
                this.dispatchLobbyEvent(lobbyEvent);
            });

            try {
                await connection.start()
                await connection.invoke("JoinLobby", this.joinCode);
            } catch (err) {
                console.error(err);
            }
        }

        notifyOfDrink(lobbyEvent: LobbyEvent) {
            this.pendingDrinks.push(lobbyEvent);

            if (this.pendingDrinks.length === 1)
                this.handleDrinkAnimationQueue();
        }

        handleDrinkAnimationQueue() {
            if (this.pendingDrinks.length === 0) return;
            this.currentDrink = this.pendingDrinks[0];

            if (this.notificationPermissionsGranted)
                new Notification('🍺 Drink!', { body: `Courtesy of ${this.getSenderNameByLobbyEvent(this.currentDrink)}` });

            setTimeout(() => {
                this.pendingDrinks.splice(0, 1);
                this.currentDrink = null;
                this.$nextTick(() => this.handleDrinkAnimationQueue());
            }, 5000);
        }

        getSenderNameByLobbyEvent(lobbyEvent: LobbyEvent): string | null {
            if (!lobbyEvent.lobbyMemberId) return null;

            return this.lobby.members.find(m => m.id === lobbyEvent.lobbyMemberId)?.name ?? null;
        }

        async dispatchLobbyEvent(lobbyEvent: LobbyEvent) {
            const currentMember = this.lobby.members.find(m => m.userId === this.currentUserId);
            if (lobbyEvent.lobbyEventType != LobbyEventType.NewPick || lobbyEvent.lobbyMemberId !== currentMember.id) {
                await this.getLobby(this.joinCode);
                if (!this.lobby) return;
            }

            if (lobbyEvent.lobbyEventType === LobbyEventType.UserRemoved && lobbyEvent.lobbyMemberId === currentMember.id) {
                this.$toast.error("You were removed from the lobby.");
                this.$router.push({ name: "Home" });
            }

            this.parseDates(lobbyEvent);

            this.addLobbyEvent(lobbyEvent);
            this.mappedLobbyEvents.push(this.replaceTemplatedStrings(lobbyEvent));

            const eventType = LobbyEventType[lobbyEvent.lobbyEventType];
            const handler = this[`on${eventType}` as keyof LobbyComponent];
            if (handler)
                handler(lobbyEvent);
        }

        replaceTemplatedStrings(lobbyEvent: LobbyEvent): LobbyEvent {
            const clone = { ...lobbyEvent };

            const templates = [
                {
                    strings: ["{{name}}", "{{senderName}}"],
                    fill: (string: string, text: string): string => {
                        const name = this.lobby.members.find(m => m.id === lobbyEvent.lobbyMemberId)?.name ?? "(name)";
                        return text.replace(string, `<strong>${name}</strong>`);
                    }
                },
                {
                    strings: ["{{recipientName}}"],
                    fill: (string: string, text: string): string => {
                        const name = this.lobby.members.find(m => m.id === lobbyEvent.lobbyMember2Id)?.name ?? "(recipient)";
                        return text.replace(string, `<strong>${name}</strong>`);
                    }
                },
                {
                    strings: ["{{player}}", "{{newScorer}}"],
                    fill: (string: string, text: string): string => {
                        const teams = this.games.find(g => g.gamePk === lobbyEvent.gamePk)?.liveData.boxscore.teams;
                        if (!teams) return text;

                        const player = [...Object.values(teams.away.players), ...Object.values(teams.home.players)].find(p => p.person.id === lobbyEvent.playerId)?.person.fullName ?? "(player)";
                        return text.replace(string, `<strong>${player}</strong>`);
                    }
                },
                {
                    strings: ["{{player2}}", "{{oldScorer}}"],
                    fill: (string: string, text: string): string => {
                        const teams = this.games.find(g => g.gamePk === lobbyEvent.gamePk)?.liveData.boxscore.teams;
                        if (!teams) return text;

                        const player = [...Object.values(teams.away.players), ...Object.values(teams.home.players)].find(p => p.person.id === lobbyEvent.player2Id)?.person.fullName ?? "(player)";
                        return text.replace(string, `<strong>${player}</strong>`);
                    }
                },
                {
                    strings: ["{{playerBadge}}"],
                    fill: (string: string, text: string): string => {
                        const teams = this.games.find(g => g.gamePk === lobbyEvent.gamePk)?.liveData.boxscore.teams;
                        if (!teams) return text;

                        const team = lobbyEvent.teamId === teams.home.team.id
                            ? teams.home
                            : teams.away;

                        let logo = "";

                        try {
                            logo = require(`@/assets/img/logos/${team.team.abbreviation}_LIGHT.png`);
                        } catch {
                            logo = require(`@/assets/img/logos/${team.team.abbreviation}.png`);
                        }

                        const img = `<img style='height: 27px; width: 27px; margin-left: -20px; margin-right: -1px; margin-top: -12px; margin-bottom: -10px;' src="${logo}" />`;
                        const teamColor = TeamColorLookup[team.team.id];

                        const playerLastName = Object.values(team.players).find(p => p.person.id === lobbyEvent.playerId)?.person.fullName.split(" ")[1];
                        return text.replace(string, `<span class='d-inline-block pl-3 ml-1 badge text-uppercase text-shadow' style='align-self: center; background-color: ${teamColor} !important;'>${img} ${playerLastName}</span>`);
                    }
                }
            ];

            clone.text = templates.reduce((text, template) =>
                template.strings.reduce((thisText, string) => {
                    if (text.includes(string)) return template.fill(string, thisText)
                    else return thisText;
                }, text),
                clone.text);

            return clone;
        }

        isIsoDateString(value: object): boolean {
            const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/;
            return value && typeof value === "string" && isoDateFormat.test(value);
        }

        parseDates(lobbyEvent: LobbyEvent) {
            if (lobbyEvent === null || lobbyEvent === undefined || typeof lobbyEvent !== "object")
                return lobbyEvent;

            for (const key of Object.keys(lobbyEvent)) {
                let value = lobbyEvent[key];
                if (this.isIsoDateString(value)) {
                    if (!value.endsWith("Z")) value += "Z";
                    lobbyEvent[key] = parseISO(value);
                }
                else if (typeof value === "object") this.parseDates(value);
            }
        }

        //LOBBY EVENTS
        async onDrinkAssigned(lobbyEvent: LobbyEvent) {
            if (lobbyEvent.lobbyMember2Id === this.currentLobbyMember?.id)
                this.notifyOfDrink(lobbyEvent);
        }

        async onDrinkAwarded(lobbyEvent: LobbyEvent) {
            if (lobbyEvent.lobbyMemberId === this.currentLobbyMember?.id)
                this.notifyCurrentUserOfCorrectPick(lobbyEvent);
        }

        getSenderName(lobbyEvent: LobbyEvent) {
            const sender = this.lobby.members.find(m => m.id === lobbyEvent.lobbyMember2Id) ?? null;
            if (sender == null) return "Anonymous";

            return sender.name;
        }

        notifyCurrentUserOfCorrectPick(lobbyEvent: LobbyEvent) {
            this.$toast.success(`<span class='fs-3'><strong class= text-uppercase'>Nailed it!</strong> You've been awarded a drink!</span>`, { duration: 5000 });
            if (this.notificationPermissionsGranted) {
                const players = this.games.flatMap(g => Object.values(g.gameData.players));
                const player = players.find(p => p.id === lobbyEvent.playerId);
                if (player) {
                    new Notification('🚨 Give out a drink!', { body: `${player.fullName} shoots and scores!` });
                } else {
                    new Notification('🚨 Give out a drink!');
                }
            }
        }

        //COMPUTED
        get feedItems() {
            const desiredEventTypes = [EventType.Goal, EventType.PeriodStart, EventType.PeriodEnd, EventType.GameEnd, EventType.Challenge, EventType.Penalty];
            const gameItems = this.games.flatMap(game => game.liveData.plays.allPlays.reduce((items: Array<FeedItem>, play) => {
                if (desiredEventTypes.includes(play.result.eventTypeId) && play.about.dateTime >= this.lobby.created)
                    return [...items, FeedItem.fromPlay(game.gamePk, game.liveData.linescore.teams, play)];
                else
                    return items;
            }, []));

            const lobbyItems = this.mappedLobbyEvents.map(evt => FeedItem.fromLobbyEvent(evt));

            const feedItems = [...gameItems, ...lobbyItems];

            feedItems.sort((a, b) => Number(a.time) - Number(b.time));
            return feedItems;
        }

        get loadingMessage(): string {
            return LoadingMessages.random();
        }

        get goals() {
            const goals = this.games.flatMap(game => game.liveData.plays.allPlays
                .filter(play => play.result.eventTypeId === EventType.Goal)
                .map(play => ({ ...play, gamePk: game.gamePk }))
            ) as Array<ScoringPlay & { gamePk: number }>;
            return goals;
        }

        get picks() {
            return this.lobby?.members.flatMap(m => m.picks);
        }

        get isGameView() {
            return this.view === "game";
        }

        get isFeedView() {
            return this.view === "feed";
        }

        get isLobbyView() {
            return this.view === "lobby";
        }

        get currentLobbyMember() {
            return this.lobby.members.find(m => m.userId === this.currentUserId);
        }
    }
</script>

<style scoped lang="scss">
    @keyframes bouncein {
        0% {
            transform: translate(-50%, -50%) scale(0.4);
            opacity: 0;
            animation-timing-function: cubic-bezier(0.34, 1.56, 0.64, 1);
        }

        25% {
            transform: translate(-50%, -50%) scale(1.08);
            opacity: 1;
            animation-timing-function: ease-out;
        }

        100% {
            transform: translate(-50%, -50%) scale(1);
            animation-timing-function: ease-in-out;
        }
    }


    .drink-animation {
        position: absolute;
        z-index: 99;
        max-width: 100%;
        width: 375px;
        max-height: 100%;
        height: 375px;
        top: 50%;
        left: 50%;
        transform: translate(-50%,-50%);
        background-color: map-get($theme-colors, "amber-500");
        border-radius: 20px;
        box-shadow: 0 0 25px black;
        animation: bouncein;
        animation-duration: 2.5s;
        animation-iteration-count: 2;
        animation-direction: alternate;
        animation-fill-mode:forwards;
    }
</style>