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
            <div class="ml-auto d-flex">
                <a role="button" v-if="show !== 'feed'" @click="show = 'feed'" class="fs-5 p-3 text-stone-400 d-block m-n3 font-weight-bold text-uppercase" style="text-decoration: none !important;">Back</a>
                <a role="button" v-if="show === 'feed'" @click="show = 'list'" class="p-3 text-stone-400 d-block my-n3 mx-3" style="text-decoration: none !important;"><i class="fs-3 fi fi-rr-list mb-n2 d-block"></i></a>
                <a role="button" v-if="show === 'feed'" @click="show = 'settings'" class="p-3 text-stone-400 d-block m-n3" style="text-decoration: none !important;"><i class="fs-3 fi fi-sr-settings mb-n2 d-block"></i></a>
            </div>
        </div>

        <div class="d-flex flex-column-reverse flex-grow-1" style="justify-content: flex-end">
            <div v-if="show == 'settings'" class="flex-grow-1 bg-stone-100">
                <div class="font-weight-bold text-uppercase text-center border py-2 bg-stone-0" style="border-bottom: none !important;">Feed Settings</div>
                <div class="p-4 fs-6">
                    <div class="font-weight-bold">
                        <span>Game Events</span>
                        <a role="button" v-if="!allGameEventsOn" class="text-decoration-none text-primary fs-8 ml-3" @click="showAllGameEvents">Show All</a>
                        <a role="button" v-if="allGameEventsOn" class="text-decoration-none text-primary fs-8 ml-3" @click="hideAllGameEvents">Hide All</a>
                    </div>
                    <div class="py-3 pl-4">
                        <b-form-checkbox v-model="filters.showGoals" name="check-button" size="lg" class="mb-3" @change="setFilters" switch>Goal</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPenalties" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Penalty</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPeriodStarts" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Period Start</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPeriodEnds" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Period End</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showGameEnds" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Game End</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showChallenges" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Coach's Challenge</b-form-checkbox>
                    </div>

                    <div class="font-weight-bold">
                        <span>Lobby Events</span>
                        <a role="button" v-if="!allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ml-3" @click="showAllLobbyEvents">Show All</a>
                        <a role="button" v-if="allLobbyEventsOn" class="text-decoration-none text-primary fs-8 ml-3" @click="hideAllLobbyEvents">Hide All</a>
                    </div>
                    <div class="py-3 pl-4">
                        <b-form-checkbox v-model="filters.showUserJoin" name="check-button" size="lg" class="mb-3" @change="setFilters" switch>User Joined</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showNameChange" name="check-button" size="lg" class="mb-3" @change="setFilters" switch>Name Change</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showPicks" name="check-button" size="lg" class="my-3" @change="setFilters" switch>Pick Player</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showDrinkAwarded" name="check-button" size="lg" @change="setFilters" class="my-3" switch>Drink Awarded</b-form-checkbox>
                        <b-form-checkbox v-model="filters.showDrinkAssigned" name="check-button" size="lg" @change="setFilters" class="my-3" switch>Drink Assigned</b-form-checkbox>
                    </div>
                </div>
            </div>

            <template v-if="show == 'list'">
                <div class="bg-stone-100">
                    <div class="font-weight-bold text-uppercase text-center border py-2 bg-stone-0" style="border-bottom: none !important;">Drink Timeline</div>
                    <table class="w-100 border">
                        <tbody>
                            <tr v-for="drink in assignedDrinks" :key="drink.id" class="border">
                                <td class="p-2 text-stone-500" style="width: 80px;">{{ drink.assigned | time }}</td>
                                <td class="p-2 font-weight-bold text-right" style="width: 0;"><span style="white-space: pre;">{{ getNameByDrink(drink) }}</span></td>
                                <td class="p-2" style="width: 0;">
                                    <div class="d-flex">
                                        <span class="fs-6 d-block">🍺</span>
                                        <i class="fi fi-sr-arrow-right fs-5 d-block mb-n2"></i>
                                    </div>
                                </td>
                                <td class="p-2 font-weight-bold">{{ getRecipientNameByDrink(drink) }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </template>

            <template v-if="show == 'feed'">
                <FeedItemComponent v-for="(item, idx) in filteredItems" :key="idx" :item="item "></FeedItemComponent>

                <!--<div v-if="items.length === 0" class="align-self-center flex-grow-1 d-flex align-items-center">
                    <div class="fs-4 text-stone-400 p-5 text-center" style="font-weight: 300;">
                        <span v-if="games.length">
                            <span class="d-block">The action starts at</span>
                            <span class="d-block mt-n2 fs-1 font-weight-bold">{{ new Date(games[0].gameData.datetime.dateTime).toLocaleTimeString("en-US", { timeZone: "America/New_York", hour: 'numeric', minute: '2-digit' }) }} EST</span>
                        </span>
                    </div>
                </div>-->
            </template>
        </div>
    </div>

</template>

<script lang="ts">
    import { Component, Vue, Prop } from 'vue-property-decorator';
    import { mapActions, mapState } from 'vuex';
    import GameScoreboard from '@/components/GameScoreboard.vue';
    import FeedItem from '@/models/feedItem';
    import FeedItemComponent from '@/components/FeedItem.vue';
    import LobbyEventType from '@/enums/lobbyEventType';
    import EventType from '@/models/nhlApi/enums/eventType';
    import parseISO from 'date-fns/parseISO';
    import format from 'date-fns/format';

    @Component({
        components: { GameScoreboard, FeedItemComponent },
        methods: {
            ...mapActions('games', ['getGames'])
        },
        computed: {
            ...mapState('lobby', ['lobby'])
        },
        filters: {
            time(t: Date | string) {
                if (typeof (t) === "string")
                    t = parseISO(t);
                return format(t, "p");
            }
        }
    })
    export default class Feed extends Vue {
        @Prop()
        items!: Array<FeedItem>;

        lobby!: Lobby;

        get filteredItems() {
            return this.items.filter((item, idx, array) => {
                const includedInFilters =
                    (item.subType === EventType.Goal && this.filters.showGoals) ||
                    (item.subType === EventType.Penalty && this.filters.showPenalties) ||
                    (item.subType === EventType.PeriodStart && this.filters.showPeriodStarts) ||
                    (item.subType === EventType.PeriodEnd && this.filters.showPeriodEnds) ||
                    (item.subType === EventType.Challenge && this.filters.showChallenges) ||
                    (item.subType === EventType.GameEnd && this.filters.showGameEnds) ||
                    (item.subType === LobbyEventType.UserJoined && this.filters.showUserJoin) ||
                    (item.subType === LobbyEventType.UserNameChanged && this.filters.showNameChange) ||
                    (item.subType === LobbyEventType.NewPick && this.filters.showPicks) ||
                    (item.subType === LobbyEventType.DrinkAwarded && this.filters.showDrinkAwarded) ||
                    (item.subType === LobbyEventType.DrinkAssigned && this.filters.showDrinkAssigned) ||
                    (item.subType === LobbyEventType.DrinkInvalidated) ||
                    (item.subType === LobbyEventType.DrinkRevoked) ||
                    (item.subType === LobbyEventType.GoalChanged) ||
                    (item.subType === LobbyEventType.UserRemoved) ||
                    (item.subType === LobbyEventType.UserRejoined);

                const isDuplicate = array[idx+1] && (item.subType === EventType.PeriodEnd && array[idx + 1].subType === EventType.GameEnd && array[idx + 1].gamePk === item.gamePk);

                return includedInFilters && !isDuplicate;
            });
        }

        show = "feed";

        filters = {
            showGoals: true,
            showPenalties: true,
            showPeriodStarts: true,
            showPeriodEnds: true,
            showChallenges: true,
            showGameEnds: true,
            showUserJoin: true,
            showNameChange: true,
            showPicks: true,
            showDrinkAwarded: true,
            showDrinkAssigned: true
        };

        created() {
            this.getFilters();
        }

        setFilters() {
            localStorage.setItem('feedFilters', JSON.stringify(this.filters));
        }

        getFilters() {
            const existingFilters = localStorage.getItem('feedFilters');
            if (existingFilters)
                this.filters = { ...this.filters, ...JSON.parse(existingFilters) };
        }

        getNameByDrink(drink: Drink) {
            const member = this.lobby.members.find(m => {
                const pick = m.picks.find(p => p.id === drink.lobbyMemberPickId);
                if (!pick) return;
                return pick;
            })

            if (member) return member.name;
            return "";
        }

        getRecipientNameByDrink(drink: Drink) {
            const member = this.lobby.members.find(m => m.id === drink.recipientLobbyMemberId);
            if (member) return member.name;

            return null;
        }

        showAllGameEvents() {
            this.filters = {
                ...this.filters,
                showGoals: true,
                showPenalties: true,
                showPeriodStarts: true,
                showPeriodEnds: true,
                showChallenges: true,
                showGameEnds: true
            }
            this.setFilters();
        }

        hideAllGameEvents() {
            this.filters = {
                ...this.filters,
                showGoals: false,
                showPenalties: false,
                showPeriodStarts: false,
                showPeriodEnds: false,
                showChallenges: false,
                showGameEnds: false
            }
            this.setFilters();
        }

        showAllLobbyEvents() {
            this.filters = {
                ...this.filters,
                showUserJoin: true,
                showNameChange: true,
                showPicks: true,
                showDrinkAwarded: true,
                showDrinkAssigned: true
            }
            this.setFilters();
        }

        hideAllLobbyEvents() {
            this.filters = {
                ...this.filters,
                showUserJoin: false,
                showNameChange: false,
                showPicks: false,
                showDrinkAwarded: false,
                showDrinkAssigned: false
            }
            this.setFilters();
        }

        get assignedDrinks() {
            return this.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).filter(d => d.recipientLobbyMemberId).sort((a, b) => Number(b.created) - Number(a.created));
        }

        get allGameEventsOn() {
            return this.filters.showGoals &&
                this.filters.showPenalties &&
                this.filters.showPeriodStarts &&
                this.filters.showPeriodEnds &&
                this.filters.showChallenges &&
                this.filters.showGameEnds;
        }

        get allLobbyEventsOn() {
            return this.filters.showUserJoin &&
                this.filters.showNameChange &&
                this.filters.showPicks &&
                this.filters.showDrinkAwarded &&
                this.filters.showDrinkAssigned
        }
    }
</script>

<style scoped>
</style>
