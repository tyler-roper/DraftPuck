<template>

    <div class="d-flex align-items-center bg-stone-100 feed-item" :class="getClassByType()" :style="getItemStyling()">
        <div class="team-icons p-3 ml-n2 flex-shrink-0" style="width: 140px;">
            <i v-if="isLobbyEvent" class="d-block fs-2 pl-3 mb-n1 pt-2 fi" :class="getIcon()"></i>
            <img v-for="(image,idx) in item.images" :key="idx" :src="logo(image)" />
        </div>
        <div class="flex-grow-1 px-4 py-3 feed-item-content" style="margin-left: -70px">
            <div class="d-flex justify-content-between header">
                <span class="d-block font-weight-bold text-uppercase header-text">{{ item.title }}</span>
                <span class="d-block timestamps" style="opacity: 0.7">
                    <span>{{ item.subtext }}</span>
                    <span v-if="item.subtext" class="mx-2">|</span>
                    <span>{{ item.time | time }}</span>
                </span>
            </div>
            <span class="d-block event-text mt-1" v-html="item.text"></span>
        </div>
    </div>

</template>

<script lang="ts">
    import { Component, Prop, Vue } from 'vue-property-decorator';
    import FeedItem from '@/models/feedItem';
    import format from 'date-fns/format';
    import EventType from '@/models/nhlApi/enums/eventType';
    import FeedItemType from '@/enums/feedItemType';
    import LobbyEventType from '@/enums/lobbyEventType';

    @Component({
        filters: {
            time(t: Date) { return format(t, "p"); }
        }
    })
    export default class FeedItemComponent extends Vue {
        @Prop()
        item!: FeedItem;

        getItemStyling() {
            if (this.item.subType === EventType.Goal)
                return { 'background-color': `${this.item.teamColor} !important`, 'color': 'white !important' }
            else if (this.item.type === FeedItemType.LobbyEvent && this.item.subType !== LobbyEventType.DrinkAssigned) {
                return { 'background-color': `#007bff !important`, 'color': 'white !important' }
            } else if (this.item.subType === LobbyEventType.DrinkAssigned) {
                return {
                    'background-image': `linear-gradient(rgba(245, 158, 11, 0.65) 0%, rgba(151, 95, 0, 0.65) 100%), url(${require("@/assets/img/beer-bg.png")})`,
                    'color': 'white !important',
                    'text-shadow': '0  1px 0 black',
                    'font-size': '16px !important'
                }
            }

            return {};
        }

        logo(img: string): string {
            return require(`@/assets/img/logos/${img}`);
        }

        getClassByType(): string {
            if (this.item.type === FeedItemType.LobbyEvent) {
                return `item-type-subtype-${this.item.subType}`;
            } else {
                return `item-type-${this.item.subType.toLowerCase()}`;
            }
        }

        getIcon(): string {
            if (this.item.type !== FeedItemType.LobbyEvent) return "";

            const icons: { [key: number ]: string } = {
                [LobbyEventType.UserJoined]: "fi-sr-user-add",
                [LobbyEventType.NewPick]: "fi-rr-badge-check",
                [LobbyEventType.DrinkAssigned]: "fi-sr-beer",
                [LobbyEventType.NewDrink]: "fi-rr-beer"
            }

            return icons[this.item.subType];
        }

        get isLobbyEvent() {
            return this.item.type === FeedItemType.LobbyEvent;
        }
    }
</script>