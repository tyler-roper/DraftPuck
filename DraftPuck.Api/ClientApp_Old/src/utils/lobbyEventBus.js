export default class LobbyEventBus extends Vue {
    getItemStyling(item) {
        if (item.subType === EventType.Goal)
            return { 'background-color': `${item.teamColor} !important`, 'color': 'white !important' };
        else
            return {};
    }
    logo(img) {
        return require(`@/assets/img/logos/${img}`);
    }
}
//# sourceMappingURL=lobbyEventBus.js.map