export default class LobbyEventBus extends Vue {
    getItemStyling(item: FeedItem) {
        if (item.subType === EventType.Goal)
            return { 'background-color': `${item.teamColor} !important`, 'color': 'white !important' }
        else
            return {};
    }

    logo(img: string) {
        return require(`@/assets/img/logos/${img}`);
    }
}