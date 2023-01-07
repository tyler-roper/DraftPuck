<template>
    <div class="d-flex overflow-hidden flex-column" style="height: 100%;">
        <template v-if="!isInvalidLobby">

            <div class="bg-stone-800 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center" style="z-index: 10;">
                <span class="fs-1 banner-logo">DraftPuck</span>

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
                    {{ getSenderNameByDrink(currentDrink) }}
                </span>
            </span>
            
        </div>

    </div>
</template>

<script lang="ts">
    import { Component, Vue, Watch, Prop, Ref } from 'vue-property-decorator';
    import addHours from 'date-fns/addHours';
    import addMinutes from 'date-fns/addMinutes';
    import format from 'date-fns/format';
    import LobbyOverview from '@/components/LobbyOverview.vue';
    import FullScoreboard from '@/components/FullScoreboard.vue';
    import Feed from '@/components/Feed.vue';
    import NHL from '@/services/NhlApiService';
    import FeedItem from '@/models/feedItem';
    import * as jsonpatch from 'fast-json-patch';
    import EventType from '@/models/nhlApi/enums/eventType';
    import GameStatusCode from '@/models/nhlApi/enums/gameStatusCode';
    import LoadingMessages from '@/models/loadingMessages';
    import PlayerType from '@/models/nhlApi/enums/playerType';
    import LobbyEventType from '@/enums/lobbyEventType';
    import LobbyService from '@/services/LobbyService';
    import { mapState, mapActions, mapGetters } from 'vuex';
    import VueResizable from 'vue-resizable'
    import '@/extensions/arrayExtensions';
    import * as signalR from "@microsoft/signalr";


    type View = "feed" | "game" | "lobby";

    @Component({
        components: { LobbyOverview, FullScoreboard, Feed, VueResizable },
        computed: {
            ...mapState('lobby', ['lobby', 'currentUserId']),
            ...mapGetters('lobby', ['isLobbyAdmin'])
        },
        methods: { ...mapActions('lobby', ['getLobby']) }
    })
    export default class LobbyComponent extends Vue {
        //PROPS
        @Prop()
        joinCode!: string;

        //STORE
        lobby!: Lobby;
        currentUserId!: string;
        getLobby!: (joinCode: string) => void;
        isLobbyAdmin!: boolean;

        //DATA
        sessionStart: Date = new Date();
        games: Array<LiveGame> = [];

        selectedDate: string = format(addHours(new Date(), -10), 'yyyy-MM-dd');

        isInvalidLobby = false;
        isLoading = false;

        timers: Array<number> = [];
        eventBus = new Vue();
        connection: HubConnection | null = null;
        lobbyEvents: Array<LobbyEvent> = [];
        view: View = "game";

        pendingDrinks: Array<Drink> = [];
        currentDrink: Drink | null = null;

        @Ref('overview') overview!: LobbyOverview;

        //METHODS
        async created() {
            try {
                this.isLoading = true;
                await this.getLobby(this.joinCode);
                this.selectedDate = format(addHours(this.lobby.created, -10), 'yyyy-MM-dd');

                const currentLobbyMember = this.lobby.members.find(m => m.userId === this.currentUserId);
                const name = currentLobbyMember?.name ?? null;

                if (!currentLobbyMember) {
                    localStorage.setItem('latestLobby', JSON.stringify({ joinCode: this.lobby.joinCode, name }))
                    this.$router.push({ name: 'Home' });
                }

                await this.connectToEventSource();
                await this.setGames();
                this.getMissedDrinks();
            } catch (e) {
                console.error(e);
                this.isInvalidLobby = true;
                return;
            } finally {
                this.isLoading = false;
            }
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
                .build();

            connection.on("LobbyEvent", (lobbyEvent: LobbyEvent) => {
                this.dispatchLobbyEvent({ ...lobbyEvent, time: new Date() });
            });

            try {
                await connection.start()
                await connection.invoke("JoinLobby", this.joinCode);
            } catch (err) {
                console.error(err);
            }
        }

        notifyOfDrink(drink: Drink) {
            this.pendingDrinks.push(drink);

            if (this.pendingDrinks.length === 1)
                this.handleDrinkAnimationQueue();
        }

        handleDrinkAnimationQueue() {
            if (this.pendingDrinks.length === 0) return;
            this.currentDrink = this.pendingDrinks[0];

            setTimeout(() => {
                this.pendingDrinks.splice(0, 1);
                this.currentDrink = null;
                this.$nextTick(() => this.handleDrinkAnimationQueue());
            }, 5000);
        }

        getSenderNameByDrink(drink: Drink): string | null {
            return this.lobby.members.find(m => {
                return m.picks.flatMap(p => p.drinks).some(d => d.id === drink.id);
            })?.name ?? null;
        }

        async dispatchLobbyEvent(lobbyEvent: LobbyEvent) {
            await this.getLobby(this.joinCode);
            if (!this.lobby) return;

            this.lobbyEvents.push(lobbyEvent);

            const eventType = LobbyEventType[lobbyEvent.type];
            const handler = this[`on${eventType}` as keyof LobbyComponent];
            if (handler)
                handler(lobbyEvent.entityId);
        }

        async onDrinkAssigned(drinkId: string) {
            const drink = this.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(d => d.id === drinkId);
            if (drink && drink.recipientLobbyMemberId === this.currentLobbyMember?.id)
                this.notifyOfDrink(drink);
        }

        async onNewDrink(drinkId: string) {
            const drink = this.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(d => d.id === drinkId);
            if (!drink) return;

            const pick = this.lobby.members.flatMap(m => m.picks).find(p => p.drinks.includes(drink));
            if (!pick) return;

            if (pick.lobbyMemberId != this.currentLobbyMember?.id) return;

            const goal = this.goals.find(g => g.gamePk === pick?.gamePk && g.about.eventId === drink.eventId);
            if (!goal) return;

            const scorer = goal.players.find(p => p.playerType === PlayerType.Scorer);
            if (!scorer) return;

            this.notifyCurrentUserOfCorrectPick(scorer);
        }

        async getMissedDrinks() {
            const picks = this.lobby.members.flatMap(m => m.picks);
            const drinks = picks.flatMap(p => p.drinks);
            this.goals.forEach(g => {
                const scorer = g.players.find(player => player.playerType === PlayerType.Scorer);
                if (!scorer) return;

                const unawardedPicks = picks
                    .filter(p => {
                        const playerIdMatches = p.gamePk === g.gamePk && p.playerId === scorer.player.id
                        const pickWasBeforeGoal = p.created <= g.about.dateTime;
                        const notAlreadyGiven = drinks.every(d => d.eventId !== g.about.eventId);
                        return playerIdMatches && pickWasBeforeGoal && notAlreadyGiven;
                    });

                unawardedPicks.forEach(async pick => {
                    const drink = await LobbyService.newDrink(this.joinCode, pick.id, g.about.eventId);
                    const lobbyMemberId = picks.find(pick => drink.lobbyMemberPickId === pick.id)?.lobbyMemberId ?? null;
                    if (lobbyMemberId == null) return;

                    const picker = this.lobby.members.find(member => member.id === lobbyMemberId);
                    if (picker == null) return;

                    if (picker.isBot) {
                        this.botAssignDrink(drink);
                    }
                });
            });
        }

        notifyCurrentUserOfCorrectPick(scorer: PlayPlayer) {
            this.$toast.success(`<span class='fs-3'><strong class= text-uppercase'>Nailed it!</strong> You've been awarded a drink for that goal by ${scorer.player.fullName}!</span>`, { duration: 7500 })
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

            const lobbyItems = this.lobbyEvents.map(lobbyEvent => {
                let player: Player | null = null;
                let gamePk: number | null = null;
                let pick: LobbyMemberPick | null = null;
                let team: Team | null = null;

                if (lobbyEvent.type === LobbyEventType.DrinkAssigned || lobbyEvent.type === LobbyEventType.NewDrink) {
                    const drink = this.lobby?.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(drink => drink.id === lobbyEvent.entityId) ?? null;
                    pick = this.lobby?.members.flatMap(m => m.picks).find(p => p.drinks.some(d => d.id === drink?.id)) ?? null;
                } else if (lobbyEvent.type === LobbyEventType.NewPick) {
                    pick = this.lobby?.members.flatMap(m => m.picks).find(p => p.id === lobbyEvent.entityId) ?? null;
                }

                if (pick != null) {
                    const game = this.games.find(g => g.gamePk === pick?.gamePk);
                    gamePk = game?.gamePk ?? null;
                    player = game?.gameData.players[`ID${pick.playerId}`] ?? null;
                    team = game?.gameData.teams.away.id === player?.currentTeam.id
                        ? game?.gameData.teams.away ?? null
                        : game?.gameData.teams.home ?? null;
                }

                return FeedItem.fromLobbyEvent(gamePk, lobbyEvent, this.lobby, player, team);
            });

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

        //WATCHERS / HANDLERS
        @Watch('goals')
        onGoalsUpdated(newGoals: Array<ScoringPlay & { gamePk: number }>, oldGoals: Array<ScoringPlay & { gamePk: number }>) {
            oldGoals = oldGoals.filter(g => g.about.dateTime < this.sessionStart);
            newGoals = newGoals.filter(g => g.about.dateTime >= this.sessionStart);

            newGoals.forEach(newGoal => {
                const newGoalScorer = newGoal.players?.find(player => player.playerType === PlayerType.Scorer) ?? null;
                const oldGoal = oldGoals.find(oldGoal => oldGoal.result.eventCode === newGoal.result.eventCode);
                const goalAlreadyExists = !!oldGoal;

                if (goalAlreadyExists) {
                    const oldGoalScorer = oldGoal.players?.find(player => player.playerType === PlayerType.Scorer) ?? null;
                    const scorerChanged = oldGoalScorer?.player.id != newGoalScorer?.player.id;
                    if (scorerChanged) this.onScorerChanged(newGoal, oldGoal);
                } else {
                    this.onGoalScored(newGoal);
                }
            });

            oldGoals.forEach(oldGoal => {
                const newGoal = newGoals.find(newGoal => newGoal.result.eventCode === oldGoal.result.eventCode);
                const goalWasRemoved = !newGoal;
                if (goalWasRemoved) this.onGoalRemoved(oldGoal);
            });
        }

        onGoalScored(goal: ScoringPlay & { gamePk: number }) {
            const scorer = goal.players?.find(player => player.playerType === PlayerType.Scorer) ?? null;

            this.eventBus.$emit('goalScored', { goal });
            if (scorer != null) this.onGoalFirstAssigned(goal);
        }

        async onGoalFirstAssigned(goal: ScoringPlay & { gamePk: number }) {
            this.eventBus.$emit('onGoalFirstAssigned', { goal });

            const picks = this.lobby.members.flatMap(m => m.picks) as LobbyMemberPick[];
            const pick = picks.find(p => p.gamePk === goal.gamePk && p.playerId === goal.players.find(player => player.playerType === PlayerType.Scorer)?.player.id) ?? null;

            if (pick != null) {
                const picker = this.lobby.members.find(m => m.picks.includes(pick));
                const isPicker = picker && picker.userId === this.currentUserId;
                const botPickAndIsAdmin = picker && picker.isBot && this.isLobbyAdmin;

                if (!isPicker && !botPickAndIsAdmin) return;

                const drink = await LobbyService.newDrink(this.joinCode, pick.id, goal.about.eventId);

                if (botPickAndIsAdmin)
                    this.botAssignDrink(drink);
            }
        }

        botAssignDrink(drink: Drink) {
            setTimeout(() => {
                const randomMember = this.lobby.members.filter(m => !m.isBot).random();
                LobbyService.assignDrink(this.joinCode, drink.id, randomMember.id);
            }, 5000);
        }

        onScorerChanged(newGoal: ScoringPlay & { gamePk: number }, oldGoal: ScoringPlay & { gamePk: number }) {
            const newScorer = newGoal.players?.find(player => player.playerType === PlayerType.Scorer) ?? null;
            const oldScorer = oldGoal.players?.find(player => player.playerType === PlayerType.Scorer) ?? null;

            this.eventBus.$emit('scorerChanged', { newGoal, oldGoal });
            if (oldScorer == null && newScorer != null) this.onGoalFirstAssigned(newGoal);
        }

        onGoalRemoved(goal: ScoringPlay & { gamePk: number }) {
            this.eventBus.$emit('goalRemoved', { goal });
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