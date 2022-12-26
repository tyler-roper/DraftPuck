<template>
    <div class="d-flex overflow-hidden flex-column" style="height: 100%;">
        <template v-if="!invalidLobby">
            <div class="bg-stone-800 px-sm-4 px-2 py-2 shadow position-relative d-flex align-items-center" style="z-index: 10;">
                <span class="fs-1 banner-logo">BrewPuck</span>
                <b-form-datepicker v-model="date" class="ml-5 d-none d-sm-flex" style="width: 300px;"></b-form-datepicker>
                <b-form-datepicker v-model="date" size="sm" class="ml-3 d-flex d-sm-none position-relative" style="z-index: 10;" button-only></b-form-datepicker>

                <a role="button" class="ml-auto text-stone-0 font-weight-bold p-2 bg-stone-700 rounded"><i class="fi fi-rr-users mr-2"></i>{{ lobbyMembers.length }}</a>

                <a role="button" class="ml-auto badge badge-pill bg-stone-600 py-2 px-3 d-sm-none font-weight-bold text-uppercase text-stone-200" style="text-decoration: none !important;" @click="toggleDisplay">
                    <span class="mr-1">&gt;</span>
                    <span>{{ showFeed ? 'Scores' : 'Feed' }}</span>
                    <span v-if="!showFeed && unseenEvents > 0" class="ml-1 mr-n2 bg-primary text-white p-1 rounded">🚨 {{ unseenEvents }}</span>
                </a>
            </div>

            <div class="d-flex flex-grow-1 overflow-hidden">
                <template v-if="!isLoading">
                    <FullScoreboard class="full-scoreboard flex-grow-1" :class="{ 'hide-mobile': !showGames }" :games="games" :date="date" style="overflow: auto;"></FullScoreboard>
                    <Feed class="feed flex-shrink-0" :class="{ 'hide-mobile': !showFeed }" :games="games" :events="events" style="width: 400px;"></Feed>
                </template>

                <div v-if="isLoading" style="width: 100%; height: 100%;" class="d-flex align-items-center">
                    <div class="mx-auto d-flex flex-column align-items-center">
                        <b-spinner class="d-block mt-3" style="width: 150px; height: 150px;" color="white"></b-spinner>
                        <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase font-weight-bold">{{loadingMessage}}...</span>
                    </div>
                </div>
            </div>
        </template>

        <template v-if="invalidLobby">
            <div style="width: 100%; height: 100%;" class="d-flex align-items-center">
                <div class="mx-auto d-flex flex-column align-items-center">
                    <span class="text-center d-block mx-auto mt-3 fs-2 text-uppercase font-weight-bold">Sorry, this lobby is invalid.</span>
                </div>
            </div>
        </template>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Watch, Prop } from 'vue-property-decorator';
    import FullScoreboard from '@/components/FullScoreboard.vue';
    import Feed from '@/components/Feed.vue';
    import NHL from '@/services/NhlApiService';
    import { Game } from '@/models/game';
    import { GamePlay } from '@/models/game-play';
    import TeamColors from '@/models/teamColorLookup';
    import * as jsonpatch from 'fast-json-patch';
    import LobbyService from '@/services/LobbyService';

    @Component({
        components: { FullScoreboard, Feed }
    })
    export default class LobbyComponent extends Vue {
        @Prop()
        id!: string;

        lobby: Lobby | null = null;
        invalidLobby = false;

        lobbyMembers = [];

        games: Array<Game> = [];

        date: string = (() => {
            const d = new Date();
            d.setHours(d.getHours() - 10);
            return d.toISOString().split("T")[0];
        })();

        showGames = true;
        showFeed = false;
        unseenEvents = 0;

        eventSource: EventSource | null = null;

        lobbyEvents: Array<object> = [];
        gameEvents: Array<GamePlay> = [];

        get events() {
            const result = [...this.gameEvents.map(g => ({ rootEventType: "GAME_EVENT", ...g })), ...this.lobbyEvents];
            result.sort((a, b) => {
                const aDate = a.rootEventType === "GAME_EVENT"
                    ? new Date(a.about.dateTime)
                    : a.date;

                const bDate = b.rootEventType === "GAME_EVENT"
                    ? new Date(b.about.dateTime)
                    : b.date;

                return aDate - bDate;
            });

            return result;
        }

        eventTypeIds = ["goal", "penalty", "period_start", "period_end", "game_end", "challenge"];

        loadingMessages = [
            "Fixing the zamboni",
            "Grabbing another beer",
            "Finding your seats",
            "Please wait: Puck in play",
            "Shoveling snow",
            "Repairing the glass",
            "Lacin' up the skates",
            "Stacking pucks",
            "Drawing up a play",
            "Challenging something",
            "Reviewing a goal",
            "TV Timeout",
            "Checking our stats",
            "Serving a penalty",
            "Looking for a bathroom",
            "Tailgating",
            "Filling the water bottles",
            "Making a pump-up playlist",
            "Hitting the showers"
        ];

        goalTexts = [
            "lights the lamp",
            "goes bar-down",
            "puts the biscuit in the basket",
            "pots a gino",
            "tickles the twine",
            "shoots and scores",
            "nets one",
            "sends it home",
            "tucks it in",
            "adds a tally",
            "finds the loose change",
            "goes top cheddar"
        ];

        loadingMessage = this.loadingMessages[Math.floor(Math.random() * this.loadingMessages.length)];

        isLoading = true;

        mediaQuery = window.matchMedia('(max-width: 500px)');

        async created() {
            try {
                this.mediaQuery.addListener(() => {
                    this.unseenEvents = 0;
                });

                try {
                    this.isLoading = true;
                    this.lobby = await LobbyService.getLobbyById(this.id);
                    this.lobbyMembers = this.lobby.lobbyMembers;
                    this.eventSource = new EventSource(`/api/events/${this.id}`);

                    this.eventSource.addEventListener("UserJoined", event => {
                        const data = JSON.parse(event.data);
                        this.handleUserJoined(data);
                    });
                } catch (e) {
                    console.error(e);
                    this.invalidLobby = true;
                    return;
                }
                await this.initialGetGames();
            } finally {
                this.isLoading = false;
            }
        }

        handleUserJoined(data: object) {
            this.lobbyEvents.push({ ...data, date: new Date(), rootEventType: "LOBBY_EVENT" });
            this.lobbyMembers.push({ id: data.lobbyMemberId, userId: data.userId, name: data.name })
        }

        @Watch('date')
        async updateGames() {
            try {
                this.loadingMessage = this.loadingMessages[Math.floor(Math.random() * this.loadingMessages.length)];
                this.isLoading = true;
                await this.initialGetGames();
            } finally {
                this.isLoading = false;
            }
        }

        toggleDisplay() {
            this.unseenEvents = 0;
            this.showFeed = !this.showFeed;
            this.showGames = !this.showGames;
        }

        enableNotifications() {
            function checkNotificationPromise() {
                try {
                    Notification.requestPermission().then();
                } catch (e) {
                    return false;
                }

                return true;
            }

            //function handlePermission(permission) {
            // set the button to shown or hidden, depending on what the user answers
            //    notificationBtn.style.display =
            //Notification.permission === 'granted' ? 'none' : 'block';
            //}

            // Let's check if the browser supports notifications
            if (!('Notification' in window)) {
                console.log("This browser does not support notifications.");
            } else if (checkNotificationPromise()) {
                Notification.requestPermission().then((permission) => {
                    console.log(permission);
                });
            } else {
                Notification.requestPermission((permission) => {
                    console.log(permission);
                });
            }
        }

        async initialGetGames() {
            const schedule = await NHL.getSchedule(this.date);

            if (!schedule?.dates?.length || !schedule.dates[0].games?.length) return;
            this.games = [];

            for (const scheduleGame of schedule.dates[0].games) {
                if (!scheduleGame.gamePk) continue;
                const game = await this.getGame(scheduleGame.gamePk)
                this.games.push(game);
            }

            this.updateEvents(true);
            this.games.forEach(g => {
                if (Number(g.gameData.status.statusCode) < 5)
                    this.setTimeoutForRefresh(g);
            })
        }

        async getGame(gamePk: number) {
            const game = await NHL.getGameData(gamePk);
            game.animations = [];

            //set colors
            const awayTeam = game?.gameData?.teams?.away;
            const homeTeam = game?.gameData?.teams?.home;

            if (awayTeam?.id) {
                awayTeam.colors = { primary: TeamColors[awayTeam.id] };
                awayTeam.logo = require(`@/assets/img/logos/${awayTeam.abbreviation}.png`);
                try {
                    awayTeam.logoLight = require(`@/assets/img/logos/${awayTeam.abbreviation}_LIGHT.png`);
                } catch {
                    awayTeam.logoLight = require(`@/assets/img/logos/${awayTeam.abbreviation}.png`);
                }
            }

            if (homeTeam?.id) {
                homeTeam.colors = { primary: TeamColors[homeTeam.id] };
                homeTeam.logo = require(`@/assets/img/logos/${homeTeam.abbreviation}.png`);
                try {
                    homeTeam.logoLight = require(`@/assets/img/logos/${homeTeam.abbreviation}_LIGHT.png`);
                } catch {
                    homeTeam.logoLight = require(`@/assets/img/logos/${homeTeam.abbreviation}.png`);
                }
            }

            //set periods
            const gamePeriods = game?.liveData?.linescore?.periods;
            if (gamePeriods)
                for (let i = gamePeriods.length + 1; i <= 3; i++)
                    gamePeriods.push({ num: i });

            return game;
        }

        async updateGame(game: Game) {
            const gameIdx = this.games.findIndex(g => g.gamePk === game.gamePk);

            if (Number(game.gameData?.status?.statusCode < 3)) {
                //game hasn't started
                this.$set(this.games, gameIdx, await this.getGame(game.gamePk));
            } else {
                //game in progress
                const diffResults = await NHL.getGamePatch(game.gamePk, game.metaData?.timeStamp);

                diffResults.forEach(diffResult => {
                    const result = jsonpatch.applyPatch(this.games[gameIdx], diffResult.diff);
                    this.$set(this.games, gameIdx, result.newDocument);
                });

                this.updateEvents();
            }

            this.setTimeoutForRefresh(this.games[gameIdx]);
        }

        async updateEvents(isInitial = false) {
            const allEvents = this.games
                .filter(g => g?.liveData?.plays?.allPlays?.length)
                .flatMap(g => g.liveData.plays.allPlays.map(p => (
                    {
                        ...p,
                        gamePk: g.gamePk,
                        isInitial,
                        randomGoalText: this.goalTexts[Math.floor(Math.random() * this.goalTexts.length)]
                    }
                )))
                .filter(e => e?.about?.dateTime && this.eventTypeIds.some(eti => eti === (e?.result?.eventTypeId?.toLowerCase() ?? "")));

            if (isInitial) {
                this.gameEvents = allEvents;
            } else {
                //push new events
                const existingEventCodes = this.gameEvents.map(e => e.result.eventCode);
                const newEvents = allEvents.filter(e => existingEventCodes.every(eec => eec != e.result.eventCode));
                this.gameEvents.push(...newEvents);

                //handle animations
                const previousGoalsAwaitingAnimation = this.gameEvents.filter(e => e.result.eventTypeId.toLowerCase() === "goal" && e.awaitingScorer === true);
                const goals = newEvents.filter(e => e.result.eventTypeId.toLowerCase() === "goal");
                const playsToAnimate = [...previousGoalsAwaitingAnimation, ...goals];

                if (!this.showFeed) this.unseenEvents += goals.length;

                playsToAnimate.forEach(play => {
                    play.awaitingScorer = true;

                    const game = this.games.find(g => g.gamePk === play.gamePk);
                    const teamId = play.team?.id;
                    const team = game.gameData.teams.away.id === teamId
                        ? game.gameData.teams.away
                        : game.gameData.teams.home;

                    const scorer = play?.players?.find(p => p.playerType?.toLowerCase() === "scorer")?.player;
                    if (!scorer) return;

                    const player = this.getPlayerById(game, scorer.id);
                    if (!player) return;

                    play.awaitingScorer = false;

                    game.animations.push({
                        active: false,
                        team,
                        player
                    });
                })
            }
        }

        getPlayerById(game: Game, playerId: number): Player {
            return game.gameData.players[`ID${playerId}`];
        }

        setTimeoutForRefresh(game: Game) {
            const gameStatus = Number(game.gameData.status?.statusCode);
            if (gameStatus >= 5) return;

            const refreshMs = gameStatus < 3
                ? 60000
                : 10000;

            setTimeout(() => { if (game) this.updateGame(game) }, refreshMs);
        }
    }
</script>