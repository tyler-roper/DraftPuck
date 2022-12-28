<template>
    <div class="bg-stone-200 text-stone-800 overflow-hidden rounded position-relative" :class="getGameTimeStyling(game)">
        <table class="score-table w-100">

            <!-- HEADER -->
            <tr>
                <th style="width: 290px;">
                    <span class="font-weight-bold">{{ game | gameTime }}</span>
                </th>

                <th style="width: 45px;" v-for="(period,idx) in game.liveData.linescore.periods" :key="idx">
                    <span v-if="hasStarted(game)" class="font-weight-bold">{{ period.num > 3 ? period.ordinalNum : period.num }}</span>
                </th>

                <th style="width: 45px;"></th>
                <th></th>
            </tr>

            <!-- TEAMS -->
            <tr v-for="team in game.gameData.teams" :key="team.id">
                <td class="bg-stone-150" :class="{ 'text-stone-0': teamWon(game, team) }" :style="{ 'background-color': teamWon(game, team) ? `${team.colors.primary} !important` : ''}">
                    <div class="d-flex align-items-center">
                        <img :src="teamWon(game, team) ? team.logoLight : team.logo" style="width: 50px; height: 50px;" class="d-block" :style="{ filter: !hasStarted(game) || (hasEnded(game) && isTeamLosing(game, team)) ? 'grayscale(1)' : ''}" />
                        <div class="ml-2 team-name">
                            <span class="d-block">{{ team.locationName }}</span>
                            <span class="d-block text-uppercase font-weight-bold mt-n1">{{ team.teamName }}</span>
                        </div>
                        <div class="ml-2 team-abr">
                            <span class="d-block d-block text-uppercase font-weight-bold">{{ team.abbreviation }}</span>
                        </div>
                        <div v-if="!hasEnded(game) && isTeamOnPowerplay(game, team) && game.liveData.linescore.powerPlayStrength.toLowerCase() != 'even'" class="ml-auto font-weight-bold fs-8 text-stone-0 p-2 rounded text-uppercase" style="line-height: 12px" :style="{'background-color': team.colors.primary}">
                            {{ game.liveData.linescore.powerPlayStrength == "5-on-4" ? "PP" : game.liveData.linescore.powerPlayStrength }}
                        </div>
                        <div v-if="isTeamGoaliePulled(game, team)" class="ml-auto font-weight-bold fs-8 text-stone-0 p-2 rounded text-uppercase" style="line-height: 12px" :style="{'background-color': team.colors.primary}">EN</div>
                    </div>
                </td>

                <td v-for="(period,idx) in game.liveData.linescore.periods" :key="idx">
                    <span v-if="hasStarted(game) && period.num <= game.liveData.linescore.currentPeriod">{{ getScoreByPeriod(game, team, period) }}</span>
                </td>

                <td>
                    <span v-if="hasStarted(game)" class="font-weight-bold fs-6">{{ getScore(game, team) }}</span>
                </td>

                <td class="pl-4" v-html="getScorersAsString(game, team)"></td>
            </tr>
        </table>

        <div class="position-absolute" style="top: 0; left: 0; height: 100%; width: 100%;" v-for="animation in game.animations" :key="animation.id" :class="{ active: animation.active }">
            <img :src="animation.team.logoLight" class="position-absolute animate-logo animate-logo-primary" />
            <img :src="animation.team.logoLight" class="position-absolute animate-logo animate-logo-secondary" />
            <div class="position-absolute animate-bg" :style="getBackgroundColor(animation.team.colors.primary)"></div>
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
        </div>
    </div>
</template>
<script lang="ts">
    import { Component, Vue, Prop, Watch } from 'vue-property-decorator';
    import { Game } from '@/models/game';
    import { GamePeriod } from '@/models/game-period'
    import { Team } from '@/models/team';
    import { Player } from '@/models/player';

    @Component({
        filters: {
            gameTime(game: Game) {
                const status = Number(game?.gameData?.status?.statusCode);
                const gameTime = new Date(game?.gameData?.datetime?.dateTime ?? "");
                if (!gameTime) return;

                if (status === 9)
                    return "POSTPONED";

                if (status < 3)
                    return `${gameTime.toLocaleTimeString("en-US", { timeZone: "America/New_York", hour: 'numeric', minute: '2-digit' })} EST`;

                const currentPeriodTimeRemaining = game?.liveData?.linescore?.currentPeriodTimeRemaining ?? "";
                const currentPeriod = game?.liveData?.linescore?.currentPeriod ?? 0;
                const currentPeriodOrdinal = game?.liveData?.linescore?.currentPeriodOrdinal;

                if (status >= 6) {
                    let string = `${currentPeriodTimeRemaining}`;
                    if (currentPeriod > 3) string += ` (${(currentPeriodOrdinal)})`
                    return string;
                }

                if (!currentPeriodTimeRemaining.includes(":")) return `${currentPeriodTimeRemaining} - ${currentPeriodOrdinal}`;
                const timeParts = currentPeriodTimeRemaining.split(":");
                const timeWithoutLeadingZero = `${Number(timeParts[0])}:${timeParts[1]}`;
                return `${timeWithoutLeadingZero} - ${currentPeriodOrdinal}`;
            }
        }
    })
    export default class GameScoreboard extends Vue {
        @Prop()
        game!: Game;

        playNextAnimation() {
            if (this.game.animations.length === 0) return;
            const nextAnimation = this.game.animations[0]
            nextAnimation.active = true;

            setTimeout(() => {
                this.game.animations = this.game.animations.filter(a => a !== nextAnimation);
                this.playNextAnimation();
            }, 7000)
        }

        @Watch('game.animations')
        onAnimationsChange() {
            if (this.game.animations.length === 1)
                this.playNextAnimation();
        }

        getGameTimeStyling(game: Game) {
            if (this.isPostponed(game)) return {
                'is-postponed': true
                };
            if (!this.hasStarted(game)) return {};
            const isExtraTime = game.liveData?.linescore?.currentPeriod > 3;
            const isThirdPeriod = game.liveData?.linescore?.currentPeriod == 3;
            const isCloseGame = Math.abs(game.liveData?.linescore.teams.away?.goals - game.liveData?.linescore.teams.home?.goals) <= 1;
            const isLate = Number(game.liveData.linescore.currentPeriodTimeRemaining.split(":")[0]) < 5 || game.liveData.linescore.currentPeriodTimeRemaining.toLowerCase() === "end";

            return {
                'is-live': this.isLive(game),
                'is-critical': isExtraTime || (isThirdPeriod && isCloseGame && isLate)
            }
        }

        getScore(game: Game, team: Team): number {
            const homeTeamId = game.gameData?.teams?.home?.id;
            if (!homeTeamId) return 0;

            const isTeamHome = homeTeamId == team.id;

            return isTeamHome
                ? game.liveData?.linescore?.teams?.home?.goals ?? 0
                : game.liveData?.linescore?.teams?.away?.goals ?? 0;
        }

        getScoreByPeriod(game: Game, team: Team, period: GamePeriod): number {
            const homeTeamId = game.gameData?.teams?.home?.id;
            if (!homeTeamId || !period || !period.home || !period.away) return 0;

            const isTeamHome = homeTeamId == team.id;

            return isTeamHome
                ? period.home.goals ?? 0
                : period.away.goals ?? 0;
        }

        getScorersAsString(game: Game, team: Team): string {
            if (!game.liveData?.plays?.scoringPlays?.length) return "";

            const scoringPlays = game.liveData.plays.scoringPlays.map(idx => game.liveData.plays.allPlays[idx]).filter(p => p.team.id === team.id);
            const goalCountByPlayer = scoringPlays.reduce((acc, play) => {
                const partialScorer = play?.players?.find(p => p.playerType?.toLowerCase() === "scorer");
                if (!partialScorer?.player?.id) return acc;

                const scorer: Player = game?.gameData?.players[`ID${partialScorer.player.id}`] ?? {};
                if (!scorer) return acc;
                if (acc[scorer.id])
                    acc[scorer.id].goals += 1;
                else
                    acc[scorer.id] = {
                        firstInitial: scorer.firstName[0],
                        lastName: scorer.lastName,
                        goals: 1
                    }

                return acc;
            }, {} as Record<string, number>);

            const readableScoringList = Object.values(goalCountByPlayer).map(v => v.goals > 1 ? `<span class='first-letter'>${v.firstInitial}</span> ${v.lastName} (${v.goals})` : `<span class='first-letter'>${v.firstInitial}</span> ${v.lastName}`);
            return readableScoringList.join(", ");
        }

        getPlayerById(playerId: number): Player {
            return this.game.gameData.players[`ID${playerId}`];
        }

        hasStarted(game: Game) {
            return Number(game?.gameData?.status?.statusCode) >= 3 && Number(game?.gameData?.status?.statusCode) < 8;
        }

        isLive(game: Game) {
            return Number(game?.gameData?.status?.statusCode) == 3 || Number(game?.gameData?.status?.statusCode) == 4;
        }

        hasEnded(game: Game) {
            return Number(game?.gameData?.status?.statusCode) >= 5  && Number(game?.gameData?.status?.statusCode) < 8;
        }

        isPostponed(game: Game) {
            return Number(game?.gameData?.status?.statusCode) == 9;
        }

        isTeamLosing(game: Game, team: Team) {
            const isHomeTeam = game.liveData.linescore.teams.home.team.id === team.id;
            const homeScore = game.liveData?.linescore?.teams?.home?.goals ?? 0;
            const awayScore = game.liveData?.linescore?.teams?.away?.goals ?? 0;

            return isHomeTeam
                ? homeScore < awayScore
                : awayScore < homeScore;
        }

        teamWon(game: Game, team: Team) {
            return this.hasEnded(game) && !this.isTeamLosing(game, team);
        }

        getBackgroundColor(hex: string) {
            return {
                ['background-image']: `linear-gradient(rgba(0,0,0,0), rgba(0,0,0,0.5)), linear-gradient(${hex},${hex})`
            }
        }

        isTeamOnPowerplay(game: Game, team: Team) {
            if (game.liveData?.linescore?.teams?.away?.team?.id === team.id) {
                return game.liveData?.linescore?.teams?.away?.powerPlay;
            } else {
                return game.liveData?.linescore?.teams?.home?.powerPlay;
            }
        }

        isTeamGoaliePulled(game: Game, team: Team) {
            if (game.liveData?.linescore?.teams?.away?.team?.id === team.id) {
                return game.liveData?.linescore?.teams?.away?.goaliePulled;
            } else {
                return game.liveData?.linescore?.teams?.home?.goaliePulled;
            }
        }

        testAnimation(game: Game, team: Team) {
            game.animations.push({
                active: false,
                team,
                player: Object.values(game.gameData.players)[0]
            });
        }
    }
</script>

<style scoped>
    table.score-table {
        height: 100%;
    }

        table.score-table td, table.score-table th {
            padding: 10px 20px;
        }

        table.score-table th {
            background-color: rgba(0,0,0,0.02);
        }

        table.score-table tr:nth-child(3) td {
            border-top: 1px solid rgba(0,0,0,0.02);
        }

        table.score-table tr td:not(:first-child),
        table.score-table tr th:not(:first-child) {
            border-left: 1px solid rgba(0,0,0,0.02);
        }

    .is-postponed th {
        color: white !important;
        background-color: #777 !important;
    }

    .is-live:not(.is-critical) th:first-child {
        color: #dc3545 !important;
    }

    .is-live.is-critical th:first-child {
        color: white !important;
        background-color: #dc3545 !important;
        position: relative;
    }

        .is-live.is-critical th:first-child::after {
            content: 'Close game';
            display: inline-block;
            position: absolute;
            right: 15px;
            top: 50%;
            transform: translateY(-50%);
            text-transform: uppercase;
            font-size: 10px;
        }
</style>