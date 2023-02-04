<template>

    <div class="d-flex align-items-center feed-item" :class="getClassByType()" :style="getItemStyling()">
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
    import parseISO from 'date-fns/parseISO';
    import EventType from '@/models/nhlApi/enums/eventType';
    import FeedItemType from '@/enums/feedItemType';
    import LobbyEventType from '@/enums/lobbyEventType';

    @Component({
        filters: {
            time(t: Date | string) {
                if (typeof (t) === "string")
                    t = parseISO(t);
                return format(t, "p");
            }
        }
    })
    export default class FeedItemComponent extends Vue {
        @Prop()
        item!: FeedItem;

        getItemStyling() {
            if (this.item.subType === EventType.Goal)
                return { 'background-color': `${this.item.teamColor} !important`, 'color': 'white !important' }

            return {};
        }

        logo(img: string): string {
            try {
                return require(`@/assets/img/logos/${img}`);
            } catch {
                console.log(this.item);
            }
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
                [LobbyEventType.DrinkAwarded]: "fi-rr-beer",
                [LobbyEventType.GoalChanged]: "fi-rr-shuffle",
                [LobbyEventType.DrinkRevoked]: "fi-rr-comment-slash",
                [LobbyEventType.UserNameChanged]: 'fi-rr-id-badge',
                [LobbyEventType.DrinkInvalidated]: 'fi-rr-trash',
                [LobbyEventType.GoalRemoved]: "fi-sr-cross-circle"
            }

            return icons[this.item.subType];
        }

        get isLobbyEvent() {
            return this.item.type === FeedItemType.LobbyEvent;
        }
    }
</script>