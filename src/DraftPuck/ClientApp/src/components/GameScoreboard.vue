<template>
    <div class="bg-stone-200 text-stone-800 overflow-hidden rounded position-relative" :class="getGameTimeStyling()">
        <table class="score-table > tbody > tr > w-100">

            <!-- HEADER -->
            <thead>
                <tr>
                    <th style="width: 290px;">
                        <span class="font-weight-bold">{{ time }}</span>
                    </th>

                    <th style="width: 45px;" v-for="(period,idx) in linescore.periods" :key="idx">
                        <span v-if="isStarted" class="font-weight-bold">{{ period.num > 3 ? period.ordinalNum : period.num }}</span>
                    </th>

                    <th style="width: 45px;" v-if="linescore.periods.length === 0"><span v-if="isStarted" class="font-weight-bold">1</span></th>
                    <th style="width: 45px;" v-if="linescore.periods.length <= 1"><span v-if="isStarted" class="font-weight-bold">2</span></th>
                    <th style="width: 45px;" v-if="linescore.periods.length <= 2"><span v-if="isStarted" class="font-weight-bold">3</span></th>

                    <th style="width: 45px;"></th>
                    <th></th>
                </tr>
            </thead>

            <!-- TEAMS -->
            <tbody>
                <tr v-for="team in teams" :key="team.id">
                    <td class="bg-stone-150" :class="{ 'text-stone-0': teamWon(team) }" :style="{ 'background-color': teamWon(team) ? `${teamColors[team.id]} !important` : ''}">
                        <div class="d-flex align-items-center">
                            <img :src="teamWon(team) ? logoLight(team) : logo(team)" style="width: 50px; height: 50px;" class="d-block" :style="{ filter: !isStarted || (isOver && teamLosing(team)) ? 'grayscale(1)' : ''}" />
                            <div class="ml-2 team-name">
                                <span class="d-block">{{ team.locationName }}</span>
                                <span class="d-block text-uppercase font-weight-bold mt-n1">{{ team.teamName }}</span>
                            </div>
                            <div class="ml-2 team-abr">
                                <span class="d-block d-block text-uppercase font-weight-bold">{{ team.abbreviation }}</span>
                            </div>
                            <div v-if="!isOver && teamOnPowerplay(team) && linescore.powerPlayStrength.toLowerCase() != 'even'" class="ml-auto font-weight-bold fs-8 text-stone-0 p-2 rounded text-uppercase" style="line-height: 12px" :style="{'background-color': teamColors[team.id]}">
                                {{ linescore.powerPlayStrength == "5-on-4" ? "PP" : linescore.powerPlayStrength }}
                            </div>
                            <div v-if="teamGoaliePulled(team)" class="ml-auto font-weight-bold fs-8 text-stone-0 p-2 rounded text-uppercase" style="line-height: 12px" :style="{'background-color': teamColors[team.id]}">EN</div>
                        </div>
                    </td>

                    <td v-for="(period,idx) in linescore.periods" :key="idx">
                        <span v-if="isStarted && period.num <= linescore.currentPeriod">{{ getScoreByPeriod(team, period) }}</span>
                    </td>

                    <td v-if="linescore.periods.length === 0"></td>
                    <td v-if="linescore.periods.length <= 1"></td>
                    <td v-if="linescore.periods.length <= 2"></td>

                    <td>
                        <span v-if="isStarted" class="font-weight-bold fs-6">{{ getScore(team) }}</span>
                    </td>

                    <td class="pl-4" v-html="getScorerStringByTeam(team)"></td>
                </tr>
            </tbody>

            <!-- FOOTER -->
            <tfoot>
                <tr>
                    <td colspan="100" class="bg-stone-100 p-0">
                        <div class="d-flex px-3 py-2 footer-bar">
                            <a v-if="homeRoster.length || awayRoster.length" role="button" class="pt-1 d-flex font-weight-bold uppercase text-decoration-none" @click="showRoster = !showRoster">
                                <span class="d-block" style="margin-top: -3px;">Rosters</span>
                                <i v-if="!showRoster" class="d-block mb-n3 fi fi-sr-caret-right"></i>
                                <i v-if="showRoster" class="d-block mb-n3 fi fi-sr-caret-down"></i>
                            </a>

                            <span v-if="homeRoster.length === 0 && awayRoster.length === 0" class="text-stone-400 small mt-1">
                                No Rosters Yet
                            </span>

                            <span v-if="pickingStarted && ((canPickForTeam(teams.away) && awayRoster.length > 0) || (canPickForTeam(teams.home) && homeRoster.length > 0))" class="badge badge-danger text-uppercase mt-1 ml-3">Picks Available</span>
                            <span v-if="!pickingStarted" class="text-stone-600 small mt-1 mb-n1 ml-3">Picks open @ <strong>{{ pickTime | formattedDate }}</strong></span>
                        </div>
                        <VueSlideToggle :open="showRoster">
                            <div class="row inset-shadow">
                                <div v-for="team in Object.values(teams)" :key="team.id" class="roster-split col-lg-6 col-12">
                                    <div class="px-3 py-2 font-weight-bold text-stone-0 d-flex align-items-center" :style="{'background-color': teamColors[team.id]}">
                                        <img class="d-block" :src="logoLight(team)" style="width: 40px; height: 40px;" />
                                        <div class="ml-3">
                                            <span class="d-block text-uppercase">{{ team.name }}</span>
                                            <span class="d-block font-weight-normal mt-n1">Season Stats</span>
                                        </div>
                                    </div>
                                    <table style="width: 100%;" class="roster-table">
                                        <thead>
                                            <tr>
                                                <th colspan="3">
                                                    <a v-if="botsHavePicks(team) && isLobbyAdmin" @click="makeBotPicks(team)" role="button" class="text-decoration-none">Make Bot Picks</a>
                                                </th>
                                                <th class="text-right" style="width: 40px;">GP</th>
                                                <th class="text-right" style="width: 40px;">G</th>
                                                <th class="text-right" style="width: 40px;">A</th>
                                                <th class="text-right" style="width: 40px;">P</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr v-for="player in getRosterByTeam(team)" :key="player.id">
                                                <td class="text-right" style="width: 50px;">{{ player.primaryPosition.abbreviation }}</td>
                                                <td class="text-right" style="width: 40px;">{{ player.primaryNumber }} </td>
                                                <td>
                                                    <span class="text-stone-700" :class="{'font-weight-bold': getPickerName(player.id) !== null}">{{ player.fullName }}</span>
                                                    <a v-if="pickingStarted && getPickerName(player.id) === null && canPickForTeam(team)" role="button" class="text-decoration-none fs-8 font-weight-bold" @click="pick(player.id, team.id)">PICK</a>

                                                    <span v-if="getPickerName(player.id) !== null && !isCurrentUserPick(player)" class="badge badge-blue text-uppercase">
                                                        ({{ getPickerName(player.id) }})
                                                        <a v-if="isLobbyAdmin" role="button" class="ml-1" @click="getPickIdAndRemovePick(player.id)">x</a>
                                                    </span>
                                                    <span v-if="isCurrentUserPick(player)" class="badge badge-danger text-uppercase">
                                                        (You)
                                                        <a v-if="isLobbyAdmin" role="button" class="ml-1 text-white" @click="getPickIdAndRemovePick(player.id)">x</a>
                                                    </span>
                                                </td>
                                                <td class="text-right">{{ player.stats.games }}</td>
                                                <td class="text-right">{{ player.stats.goals }}</td>
                                                <td class="text-right">{{ player.stats.assists }}</td>
                                                <td class="text-right">{{ player.stats.points }}</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </VueSlideToggle>
                    </td>
                </tr>
            </tfoot>
        </table>

        <!--<div class="position-absolute" style="top: 0; left: 0; height: 100%; width: 100%;" v-for="animation in game.animations" :key="animation.id" :class="{ active: animation.active }">
            <img :src="animation.team.logoLight" class="position-absolute animate-logo animate-logo-primary" />
            <img :src="animation.team.logoLight" class="position-absolute animate-logo animate-logo-secondary" />
            <div class="position-absolute animate-bg" :style="getBackgroundColor(animation.TeamColors[team.id])"></div>
            <div class="position-absolute animate-text">{{ animation.team.abbreviation }} GOAL</div>
            <div class="position-absolute animate-text-2 align-items-center pt-4">
                <div class="animate-number">
                    #{{ animation.player.primaryNumber }}
                </div>
                <div class="ml-3 animate-name">
                    <span class="animate-first-name d-block text-uppercase">
                        {{ animation.player.firstName }}
                    </span>
                    <span class="d-block animate-last-name" style="">
                        {{ animation.player.lastName }}
                    </span>
                </div>
            </div>
        </div>-->
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch } from 'vue-property-decorator';
    import Scoreboard from '@/models/scoreboard';
    import TeamColors from '@/models/teamColorLookup';
    import NHL from '@/services/NhlApiService';
    import { VueSlideToggle } from 'vue-slide-toggle';
    import { mapState, mapActions, mapGetters } from 'vuex';
    import BotPickStyle from '@/enums/botPickStyle';
    import '@/extensions/arrayExtensions';
    import addMinutes from 'date-fns/addMinutes';
    import intervalToDuration from 'date-fns/intervalToDuration';
    import formatDuration from 'date-fns/formatDuration';
    import format from 'date-fns/format';

    @Component({
        components: { VueSlideToggle },
        computed: {
            ...mapState('lobby', ['lobby', 'currentUserId']),
            ...mapGetters('lobby', ['isLobbyAdmin']) 
        },
        methods: { ...mapActions('lobby', ['pickPlayer', 'removePick']) },
        filters: {
            formattedDate: (d: Date) => {
                return format(d, "p");
            }
        }
    })
    export default class GameScoreboard extends Vue {
        @Prop()
        scoreboard!: Scoreboard;

        lobby!: Lobby;
        pickPlayer!: (args: { gamePk: number; playerId: number; teamId: number; lobbyMemberId: string | null }) => Promise<void>;
        removePick!: (pickId: string) => Promise<void>;
        currentUserId!: string;
        isLobbyAdmin!: boolean;

        gamePk = this.scoreboard.gamePk;
        time = this.scoreboard.time;
        gameStatus = this.scoreboard.gameStatus;
        linescore = this.scoreboard.linescore;
        boxscore = this.scoreboard.boxscore;
        teams = this.scoreboard.teams;
        players = this.scoreboard.players;
        startTime = this.scoreboard.startTime;
        homeRoster: Array<Player & { stats: PlayerSeasonStats }> = [];
        awayRoster: Array<Player & { stats: PlayerSeasonStats }> = [];
        pickingStarted = false;
        pickingStartingTimer?: number;
        timeUntilPicking: string | null = null;
        pickTime = addMinutes(this.startTime, -30);

        @Watch('scoreboard')
        onScoreboardUpdate() {
            this.gamePk = this.scoreboard.gamePk;
            this.time = this.scoreboard.time;
            this.gameStatus = this.scoreboard.gameStatus;
            this.linescore = this.scoreboard.linescore;
            this.boxscore = this.scoreboard.boxscore;
            this.teams = this.scoreboard.teams;
            this.players = this.scoreboard.players;
        }

        teamColors = TeamColors;
        showRoster = false;

        async created() {
            await this.setRosterByTeam(this.teams.home);
            await this.setRosterByTeam(this.teams.away);

            this.updateTimeUntilPicking();

            if (!this.pickingStarted) {
                this.pickingStartingTimer = setInterval(() => {
                    this.updateTimeUntilPicking();
                }, 1000);
            }
        }

        updateTimeUntilPicking() {
            this.pickingStarted = this.pickTime <= new Date();
            if (this.pickingStarted) {
                clearInterval(this.pickingStartingTimer);
                return;
            }

            const duration = intervalToDuration({ start: this.pickTime, end: new Date() });
            if (duration.hours === 0 && duration.minutes == 0)
                this.timeUntilPicking = formatDuration(duration, { format: ["seconds"], zero: true });
            else
                this.timeUntilPicking = formatDuration(duration, { format: ["hours", "minutes"] });
        }

        getGameTimeStyling() {
            if (!this.isStarted) return {};

            const isExtraTime = this.linescore.currentPeriod > 3;
            const isThirdPeriod = this.linescore.currentPeriod == 3;
            const isCloseGame = Math.abs(this.linescore.teams.away.goals - this.linescore.teams.home?.goals) <= 1;
            const isLate = Number(this.linescore.currentPeriodTimeRemaining.split(":")[0]) < 5 || this.linescore.currentPeriodTimeRemaining.toLowerCase() === "end";

            return {
                'is-live': this.isInProgress,
                'is-critical': isExtraTime || (isThirdPeriod && isCloseGame && isLate)
            }
        }

        logo(team: Team) {
            return require(`@/assets/img/logos/${team.abbreviation}.png`);
        }

        logoLight(team: Team) {
            try {
                return require(`@/assets/img/logos/${team.abbreviation}_LIGHT.png`);
            } catch {
                return this.logo(team);
            }
        }

        teamPlayers(team: Team) {
            return this.players.filter(player => player.currentTeam.id === team.id);
        }

        teamLosing(team: Team) {
            const linescoreTeam = this.toLinescoreTeam(team);

            const otherTeam = this.linescore.teams.away.team.id === team.id
                ? this.linescore.teams.home
                : this.linescore.teams.away;

            return linescoreTeam.goals < otherTeam.goals;
        }

        teamWinning(team: Team) {
            const linescoreTeam = this.toLinescoreTeam(team);

            const otherTeam = this.linescore.teams.away.team.id === team.id
                ? this.linescore.teams.home
                : this.linescore.teams.away;

            return linescoreTeam.goals > otherTeam.goals;
        }

        teamWon(team: Team) {
            return this.isOver && this.teamWinning(team);
        }

        teamOnPowerplay(team: Team) {
            const linescoreTeam = this.toLinescoreTeam(team);
            return linescoreTeam.powerPlay;
        }

        teamGoaliePulled(team: Team) {
            const linescoreTeam = this.toLinescoreTeam(team);
            return linescoreTeam.goaliePulled;
        }

        toLinescoreTeam(team: Team): LineScoreTeam {
            return this.linescore.teams.away.team.id === team.id
                ? this.linescore.teams.away
                : this.linescore.teams.home;
        }

        getScoreByPeriod(team: Team, period: Period) {
            const linescorePeriod = this.linescore.periods.find(p => p.num === period.num);
            if (!linescorePeriod) return 0;

            const isHomeTeam = this.linescore.teams.home.team.id === team.id;

            return isHomeTeam
                ? linescorePeriod.home.goals
                : linescorePeriod.away.goals;
        }

        getScore(team: Team) {
            return this.toLinescoreTeam(team).goals;
        }

        getRosterByTeam(team: Team): Array<Player & { stats: PlayerSeasonStats }> {
            const isHomeTeam = this.linescore.teams.home.team.id === team.id;

            return isHomeTeam
                ? this.homeRoster
                : this.awayRoster;
        }

        getPlayersByTeam(team: Team): Array<Player> {
            const players = team.id === this.linescore.teams.home.team.id
                ? this.boxscore.teams.home.players
                : this.boxscore.teams.away.players;

            return Object.values(players).map(p => this.players.find(pp => pp.id === p.person.id)) as Array<Player>;
        }

        getScorersByTeam(team: Team): { [key: number]: number } {
            const players = team.id === this.linescore.teams.home.team.id
                ? this.boxscore.teams.home.players
                : this.boxscore.teams.away.players

            return Object.values(players).reduce((goalCounts: { [key: number]: number }, player) => {
                if (player.stats.skaterStats && player.stats.skaterStats.goals)
                    goalCounts[player.person.id] = player.stats.skaterStats.goals;

                return goalCounts;
            }, {});
        }

        getScorerStringByTeam(team: Team): string {
            const scorers = Object.entries(this.getScorersByTeam(team)).map(([playerId, goals]) => {
                const player = this.players.find(player => player.id === Number(playerId));
                if (!player) return "";

                let output = `${player.firstName[0]} ${player.lastName}`
                if (goals > 1)
                    output += ` (${goals})`;

                return output;
            });

            return scorers.join(", ");
        }

        async getPickIdAndRemovePick(playerId: number) {
            const picks = this.lobby?.members.flatMap(m => m.picks);
            const pick = picks.find(p => p.playerId === playerId);
            if (!pick) return;

            await this.removePick(pick.id);
        }

        async pick(playerId: number, teamId: number, lobbyMemberId: string | null = null) {
            if (lobbyMemberId === null) {
                lobbyMemberId = this.lobby.members.find(m => m.userId === this.currentUserId).id;
            }
            const picks = this.lobby?.members.flatMap(m => m.picks);

            const existingPick = picks?.find(p => p.gamePk === this.gamePk && p.playerId === playerId);

            if (existingPick) {
                const member = this.lobby?.members.find(m => m.picks.includes(existingPick));
                let name = member
                    ? `<strong>${member.name}</strong> has`
                    : "Someone has";

                if (member?.userId === this.currentUserId) name = "You have";

                this.$toast.error(`Oops! ${name} already picked this player.`);
                return;
            }

            await this.pickPlayer({ gamePk: this.gamePk, playerId, teamId, lobbyMemberId });
        }

        async setRosterByTeam(team: Team) {
            const isHomeTeam = this.linescore.teams.home.team.id === team.id;
            const roster = isHomeTeam
                ? this.homeRoster
                : this.awayRoster;

            const players = this.getPlayersByTeam(team);
            const playerPromises = players.map(async player => {
                const rosterPlayer = roster.find(rp => rp.id === player.id);
                if (rosterPlayer) return rosterPlayer;

                const stats = await NHL.getPlayerSeasonStats(player.id);
                return {
                    ...player,
                    stats
                };
            });

            const result = (await Promise.all(playerPromises)).filter(player => player.primaryPosition.code !== "G");

            result.sort((a, b) => b.stats.goals - a.stats.goals);
            if (isHomeTeam)
                this.homeRoster = result
            else
                this.awayRoster = result;

            if (result.length === 0) {
                //console.log(`[${this.teams.away.abbreviation} @ ${this.teams.home.abbreviation}] Roster not available for ${team.abbreviation}. Trying again in 60 seconds.`);
                setTimeout(async () => await this.setRosterByTeam(team), 60000);
            } else {
                //console.log(`[${this.teams.away.abbreviation} @ ${this.teams.home.abbreviation}] Roster retrieved for ${team.abbreviation}.`);
            }
        }

        getPickerName(playerId: number): string | null {
            return this.pickedPlayers[playerId] ?? null;
        }

        botsHavePicks(team: Team): boolean {
            const bots = this.lobby.members.filter(m => m.isBot);
            if (!bots.length) return false;
            return bots.some(bot => this.userHasPicksForTeam(bot, team));
        }

        async makeBotPicks(team: Team) {
            const botsWithPicks = this.lobby.members.filter(bot => bot.isBot && this.userHasPicksForTeam(bot, team));
            const delayBetweenBotPicks = 1000;

            botsWithPicks.forEach((bot, idx) => {
                let waitTime = idx * delayBetweenBotPicks;
                const roster = this.getRosterByTeam(team);
                const picks = bot.picks;
                if (!roster || !picks) return false;

                const picksMade = picks.filter((p: LobbyMemberPick) => p.gamePk === this.scoreboard.gamePk && roster.some(r => r.id === p.playerId)).length;
                const picksRemaining = this.lobby.picksPerTeam - picksMade;

                for (let i = 0; i < picksRemaining; i++) {
                    setTimeout(async () => await this.makeBotPick(bot, team), waitTime);
                    waitTime += botsWithPicks.length * delayBetweenBotPicks;
                }
            });
        }

        async makeBotPick(bot: LobbyMember, team: Team) {
            const style = bot.botPickStyle as BotPickStyle;
            const roster = this.getRosterByTeam(team);
            const availablePlayers = roster.filter(player => !this.pickedPlayers[player.id]);

            if (style === BotPickStyle.Best) {
                await this.pick(availablePlayers[0].id, team.id, bot.id);
            } else if (style === BotPickStyle.Good) {
                const best4 = roster.slice(0, 4);
                const availableOfBest4 = best4.filter(p => availablePlayers.includes(p));
                if (availableOfBest4.length) {
                    const playerToPick = availableOfBest4.random();
                    await this.pick(playerToPick.id, team.id, bot.id);
                } else {
                    await this.pick(availablePlayers[0].id, team.id, bot.id);
                }
            } else if (style === BotPickStyle.Average) {
                const average4 = roster.slice(5, 8);
                const availableOfAverage4 = average4.filter(p => availablePlayers.includes(p));
                if (availableOfAverage4.length) {
                    const playerToPick = availableOfAverage4.random();
                    await this.pick(playerToPick.id, team.id, bot.id);
                } else {
                    let playerToPick = null;
                    for (let i = 5; i < roster.length; i++) {
                        if (availablePlayers.includes(roster[i])) {
                            playerToPick = roster[i];
                            break;
                        }
                    }
                    if (playerToPick === null) {
                        playerToPick = availablePlayers.random();
                    }

                    await this.pick(playerToPick.id, team.id, bot.id);
                }
            } else if (style === BotPickStyle.Bad) {
                const bad5 = roster.slice(-5, -1);
                const availableOfBad5 = bad5.filter(p => availablePlayers.includes(p));
                if (availableOfBad5.length) {
                    const playerToPick = availableOfBad5.random();
                    await this.pick(playerToPick.id, team.id, bot.id);
                } else {
                    let playerToPick = null;
                    for (let i = roster.length - 6; i < roster.length; i++) {
                        if (availablePlayers.includes(roster[i])) {
                            playerToPick = roster[i];
                            break;
                        }
                    }
                    if (playerToPick === null) {
                        playerToPick = availablePlayers.random();
                    }

                    await this.pick(playerToPick.id, team.id, bot.id);
                }
            } else if (style === BotPickStyle.Worst) {
                await this.pick(availablePlayers[availablePlayers.length - 1].id, team.id, bot.id);
            } else if (style === BotPickStyle.Random) {
                await this.pick(availablePlayers.random().id, team.id, bot.id);
            }
        }

        get pickedPlayers() {
            return this.lobby?.members.reduce((playerIdLookup: { [key: number]: string }, member) => {
                member.picks.forEach(pick => playerIdLookup[pick.playerId] = member.name);
                return playerIdLookup;
            }, {});
        }

        userHasPicksForTeam(member: LobbyMember, team: Team) {
            if (this.isOver) return false;

            const roster = this.getRosterByTeam(team);
            const currentUserPicks = member.picks;
            if (!roster || !currentUserPicks) return false;
            const count = currentUserPicks.filter((p: LobbyMemberPick) => p.gamePk === this.scoreboard.gamePk && roster.some(r => r.id === p.playerId)).length;

            return count < this.lobby.picksPerTeam;
        }

        canPickForTeam(team: Team) {
            const currentMember = this.lobby.members.find(m => m.userId === this.currentUserId);
            if (!currentMember) return false;

            return this.userHasPicksForTeam(currentMember, team);
        }

        isCurrentUserPick(player: Player) {
            return this.lobby?.members.find(m => m.userId === this.currentUserId)?.picks.some((p: LobbyMemberPick) => p.gamePk === this.scoreboard.gamePk && p.playerId === player.id);
        }

        //COMPUTED
        get isInProgress() {
            return this.scoreboard.isInProgress;
        }

        get isOver() {
            return this.scoreboard.isOver;
        }

        get isStarted() {
            return this.scoreboard.isStarted;
        }
    }
</script>

<style>
    .roster-table > thead > tr > th,
    .roster-table > thead > tr > td,
    .roster-table > tbody > tr > th,
    .roster-table > tbody > tr > td {
        padding: 3px 10px;
    }

    .roster-split {
        background-color: #ebebeb;
    }

    .roster-table > thead > tr > th {
        background-color: rgba(0, 0, 0, 0.02);
    }

    .roster-table > tbody > tr:nth-child(even) > td {
        background-color: rgba(0, 0, 0, 0.01);
    }

    .footer-bar {
        position: relative;
        z-index: 2;
        box-shadow: 0 5px 5px rgba(0,0,0,0.2);
    }
</style>