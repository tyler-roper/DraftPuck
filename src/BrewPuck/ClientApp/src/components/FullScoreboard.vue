<template>
    <div>
        <div class="flex-grow-1 overflow-hidden d-flex flex-wrap p-2">
            <GameScoreboard class="game-scoreboard m-3" :game="game" style="width: calc(50% - 2rem);" v-for="game in games" :key="game.gamePk"></GameScoreboard>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue, Prop } from 'vue-property-decorator';
    import { Game } from '@/models/game';
    import GameScoreboard from '@/components/GameScoreboard.vue';

    @Component({
        components: { GameScoreboard }
    })
    export default class FullScoreboard extends Vue {
        @Prop()
        date?: string;

        @Prop()
        games!: Array<Game>;
    }
</script>