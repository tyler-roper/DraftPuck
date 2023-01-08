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
                <a role="button" @click="show = 'feed'" class="fs-3 p-3 text-stone-400 d-block my-n3 mx-3 font-weight-bold" style="text-decoration: none !important;">F</a>
                <a role="button" @click="show = 'list'" class="p-3 text-stone-400 d-block my-n3 mx-3" style="text-decoration: none !important;"><i class="fs-3 fi fi-sr-list mb-n2 d-block"></i></a>
                <a role="button" @click="show = 'settings'" class="p-3 text-stone-400 d-block m-n3" style="text-decoration: none !important;"><i class="fs-3 fi fi-sr-settings mb-n2 d-block"></i></a>
            </div>
        </div>

        <div class="d-flex flex-column-reverse flex-grow-1" style="justify-content: flex-end">
            <div v-if="show == 'settings'" class="flex-grow-1">
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

            <template v-if="show == 'list'">
                <div v-for="drink in drinks" :key="drink.id" class="py-2"><strong>{{ getNameByDrink(drink) }}</strong> gives to <strong>{{ getRecipientNameByDrink(drink) }}</strong></div>
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
    import FeedItemType from '@/enums/feedItemType';
    import EventType from '@/models/nhlApi/enums/eventType';

    @Component({
        components: { GameScoreboard, FeedItemComponent },
        methods: {
            ...mapActions('games', ['getGames'])
        },
        computed: {
            ...mapState('lobby', ['lobby'])
        }
    })
    export default class Feed extends Vue {
        @Prop()
        items!: Array<FeedItem>;

        get filteredItems() {
            return this.items.filter((item, idx, array) => {
                const includedInFilters =
                    (item.type === FeedItemType.LobbyEvent) ||
                    (item.subType === EventType.Goal && this.filters.showGoals) ||
                    (item.subType === EventType.Penalty && this.filters.showPenalties) ||
                    (item.subType === EventType.PeriodStart && this.filters.showPeriodStarts) ||
                    (item.subType === EventType.PeriodEnd && this.filters.showPeriodEnds) ||
                    (item.subType === EventType.Challenge && this.filters.showChallenges) ||
                    (item.subType === EventType.GameEnd && this.filters.showGameEnds);

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
            showGameEnds: true
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

        get drinks() {
            return this.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks);
        }
    }
</script>

<style scoped>
</style>
 