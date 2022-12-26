<template>

    <div style="overflow-y: scroll;" class="bg-stone-300 text-stone-800 d-flex flex-column">
        <div class="bg-stone-150 p-3 ls-2  shadow d-flex align-items-center" style="z-index: 2; position: sticky; top: 0;">
            <div class="fs-3 mr-2">
                🚨
            </div>
            <div>
                <span class="d-block mb-n2">The</span>
                <span class="fs-4 font-weight-bold d-block text-uppercase">Feed</span>
            </div>
            <div class="ml-auto">
                <a role="button" @click="toggleSettings" class="p-3 text-stone-400 d-block m-n3" style="text-decoration: none !important;"><i class="fs-3 fi fi-sr-settings mb-n2 d-block"></i></a>
            </div>
        </div>

        <div class="d-flex flex-column-reverse flex-grow-1" style="justify-content: flex-end">
            <div v-if="showSettings" class="flex-grow-1">
                <div class="p-4">
                    <span class="d-block fs-6">Show the following events in the feed:</span>
                    <div class="font-weight-bold text-uppercase">
                        <b-form-checkbox v-model="filters.showGoals" name="check-button" size="lg" class="my-3" switch>Goal</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPenalties" name="check-button" size="lg" class="my-3" switch>Penalty</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPeriodStarts" name="check-button" size="lg" class="my-3" switch>Period Start</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPeriodEnds" name="check-button" size="lg" class="my-3" switch>Period End</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showGameEnds" name="check-button" size="lg" class="my-3" switch>Game End</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showChallenges" name="check-button" size="lg" class="my-3" switch>Coach's Challenge</b-form-checkbox>
                    </div>
                </div>
            </div>

            <template v-if="!showSettings">
                <div v-for="(item, idx) in filteredEvents" :key="idx" class="d-flex align-items-center bg-stone-100 feed-item" :class="{'initial-load': item.isInitial === true, [`item-type-${getEventType(item)}`]: true }" :style="getBackgroundForPlay(item)">
                    <template v-if="item.rootEventType === 'GAME_EVENT'">
                        <div class="team-icons p-3 mr-n5 ml-n2 flex-shrink-0" style="width: 140px;">
                            <img v-for="team in getTeamsByPlay(item)" :key="team.id" :src="getEventType(item) == 'goal' ? team.logoLight : team.logo" />
                        </div>
                        <div class="flex-grow-1 px-4 py-3 feed-item-content">
                            <div class="d-flex justify-content-between header">
                                <span class="d-block font-weight-bold text-uppercase header-text" :class="{'text-danger': getEventType(item) === 'penalty'}" v-html="getHeaderTextForEvent(item)"></span>
                                <span class="d-block timestamps" style="opacity: 0.7" v-html="getTimestampForEvent(item)"></span>
                            </div>
                            <span class="d-block event-text mt-1" v-html="getTextForEvent(item)"></span>
                        </div>
                    </template>
                    <template v-if="item.rootEventType === 'LOBBY_EVENT'">

                        <div class="team-icons p-3 mr-n5 ml-n2 flex-shrink-0" style="width: 140px;">
                            <i class="fi fi-rr-user-add"></i>
                        </div>
                        <div class="flex-grow-1 px-4 py-3 feed-item-content">
                            <div class="d-flex justify-content-between header">
                                <span class="d-block font-weight-bold text-uppercase header-text text-blue">Welcome!</span>
                                <span class="d-block timestamps" style="opacity: 0.7" v-html="getTimestampForEvent(item)"></span>
                            </div>
                            <span class="d-block event-text mt-1"><strong>{{ item.name }}</strong> joined the lobby.</span>
                        </div>

                    </template>
                </div>

                <div v-if="events.length === 0" class="align-self-center flex-grow-1 d-flex align-items-center">
                    <div class="fs-4 text-stone-400 p-5 text-center" style="font-weight: 300;">
                        <span v-if="games.length">
                            <span class="d-block">The action starts at</span>
                            <span class="d-block mt-n2 fs-1 font-weight-bold">{{ new Date(games[0].gameData.datetime.dateTime).toLocaleTimeString("en-US", { timeZone: "America/New_York", hour: 'numeric', minute: '2-digit' }) }} EST</span>
                        </span>
                    </div>
                </div>
            </template>
        </div>
    </div>

</template>

<script lang="ts">
    import { Component, Vue, Prop } from 'vue-property-decorator';
    import { GamePlay } from '@/models/game-play';
    import { Game } from '@/models/game';
    import { Team } from '@/models/team';
    import { Player } from '@/models/player';
    import { mapActions } from 'vuex';
    import GameScoreboard from '@/components/GameScoreboard.vue';

    @Component({
        components: { GameScoreboard },
        methods: { ...mapActions('games', ['getGames']) }
    })
    export default class Feed extends Vue {
        @Prop()
        games!: Array<Game>;

        @Prop()
        events!: Array<GamePlay>;

        teams = this.games.flatMap(g => g.gameData).flatMap(gd => gd.teams).flatMap(t => [t.home, t.away]);

        showSettings = false;

        filters = {
            showGoals: true,
            showPenalties: true,
            showPeriodStarts: true,
            showPeriodEnds: true,
            showChallenges: true,
            showGameEnds: true
        };

        created() {
            this.getFilters();
        }

        toggleSettings() {
            this.events.forEach(e => e.isInitial = true);
            this.showSettings = !this.showSettings;
            if (!this.showSettings) this.setFilters();
        }

        getFilters() {
            const existingFilters = localStorage.getItem('feedFilters');
            if (existingFilters)
                this.filters = { ...this.filters, ...JSON.parse(existingFilters) };
        }

        setFilters() {
            localStorage.setItem('feedFilters', JSON.stringify(this.filters));
        }

        getTeamsByPlay(play: GamePlay): Array<Team> {
            if (play.team) return [this.teams.find(t => t?.id === play.team?.id) as Team];

            const teams = this.games.find(g => g.gamePk === play.gamePk)?.gameData?.teams;
            return [teams?.home as Team, teams?.away as Team];
        }

        getEventType(play: GamePlay): string {
            if (play.type === "LOBBY_EVENT")
                return "lobby-event"
            else
                return play?.result?.eventTypeId?.toLowerCase() ?? "";
        }

        getHeaderTextForEvent(play: GamePlay): string {
            const eventType = this.getEventType(play).toLowerCase();
            if (eventType !== "goal") return eventType.replace("_", " ");

            const homeTeam = this.games.find(g => g.gamePk === play.gamePk)?.gameData?.teams?.home;
            const awayTeam = this.games.find(g => g.gamePk === play.gamePk)?.gameData?.teams?.away;

            let output = [];
            if (play.about?.goals?.home > play.about?.goals?.away) {
                output = [play.about.goals.home, play.about.goals.away, homeTeam?.abbreviation];
            } else if (play.about?.goals?.home < play.about?.goals?.away) {
                output = [play.about.goals.away, play.about.goals.home, awayTeam?.abbreviation];
            } else {
                output = [play.about.goals.away, play.about.goals.home, "Tie"];
            }

            return `<span class='score'>${output[0]} - ${output[1]}</span> <span class='ml-2 team'>${output[2]}</span>`
        }

        getTextForEvent(play: GamePlay): string {
            const e = this.getEventType(play);

            if (e === "goal") {
                const partialScorer = play?.players?.find(p => p?.playerType?.toLowerCase() === "scorer")?.player;
                if (!partialScorer?.id) return "Waiting for details...";
                const scorer = this.getPlayerById(partialScorer.id);
                const text = play.randomGoalText;
                return `<strong>${scorer.fullName}</strong> <span class='goal-flavor-text'>${text}!</span>`;
            } else if (e === "period_start") {
                return `Start of ${play.about?.ordinalNum} period`;
            } else {
                return play.result?.description ?? "";
            }
        }

        getTimestampForEvent(play: GamePlay): string {
            if (play.rootEventType === "LOBBY_EVENT")
                return `<span class='date-time'>${play.date.toLocaleTimeString("en-US", { timeZone: "America/New_York", hour: 'numeric', minute: '2-digit' }) }</span>`;

            const timeEst = new Date(play.about.dateTime).toLocaleTimeString("en-US", { timeZone: "America/New_York", hour: 'numeric', minute: '2-digit' });
            

            const e = this.getEventType(play);
            if (e === "goal" || e === "penalty" || e === "challenge") {
                return `<span class='period'>${play.about?.periodTime} - ${play.about?.ordinalNum}<span> <span class='mx-2 divider'>|</span> <span class='datetime'>${timeEst}</span>`;
            } else {
                return `<span class='date-time'>${timeEst}</span>`;
            }
        }

        getPlayerById(playerId: number): Player {
            return this.games.flatMap(g => g?.gameData?.players).reduce((acc, playerList) => ({ ...acc, ...playerList }), {})[`ID${playerId}`] ?? {};
        }

        getBackgroundForPlay(play: GamePlay): Record<string, string> {
            const type = play.type;
            if (type === "LOBBY_EVENT") return {};

            const e = this.getEventType(play);
            if (e !== "goal") return {};

            const team = this.getTeamsByPlay(play)[0];
            return {
                'background-color': `${team.colors.primary} !important`,
                'color': 'white !important'
            }
        }

        get eventTypesFiltered() {
            const result = [];
            if (this.filters.showGoals) result.push("goal");
            if (this.filters.showPenalties) result.push("penalty");
            if (this.filters.showPeriodStarts) result.push("period_start");
            if (this.filters.showPeriodEnds) result.push("period_end");
            if (this.filters.showChallenges) result.push("challenge");
            if (this.filters.showGameEnds) result.push("game_end");

            return result;
        }

        get filteredEvents() {
            return this.events.filter(e => e.rootEventType === "LOBBY_EVENT" || this.eventTypesFiltered.includes(this.getEventType(e)));
        }
    }
</script>

<style scoped>
</style>
