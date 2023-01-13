<template>
    <div>
        <div class="flex-grow-1 overflow-hidden d-flex flex-wrap p-2" style="align-items: flex-start">
            <GameScoreboard v-for="(scoreboard, idx) in scoreboards" :key="idx" class="game-scoreboard m-3" :scoreboard="scoreboard" style="width: calc(50% - 2rem);"></GameScoreboard>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop } from 'vue-property-decorator';
    import GameScoreboard from '@/components/GameScoreboard.vue';
    import Scoreboard from '@/models/scoreboard';

    @Component({
        components: { GameScoreboard }
    })
    export default class FullScoreboard extends Vue {
        @Prop()
        games!: Array<LiveGame>;

        @Prop()
        eventBus!: Vue;

        created() {
            this.eventBus.$on('doTheThing', () => { alert("We bussin'!"); });
        }

        get scoreboards() {
            return this.games.map(game => Scoreboard.fromLiveGame(game));
        }
    }
</script>