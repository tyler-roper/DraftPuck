import LobbyService from "@/services/LobbyService";
export default {
    namespaced: true,
    state: () => ({ lobby: null, lobbyEvents: [], currentUserId: null }),
    mutations: {
        setLobby(state, lobby) {
            state.lobby = lobby;
        },
        setCurrentUserId(state, userId) {
            state.currentUserId = userId;
        },
        addPick(state, pick) {
            state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks.push(pick);
        },
        updatePick(state, pick) {
            const idx = state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks.findIndex(p => p.playerId === pick.playerId && pick.gamePk === p.gamePk);
            state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks[idx] = pick;
        },
        assignDrink(state, drink) {
            const matchingDrink = state.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(d => d.id === drink.id);
            if (matchingDrink)
                matchingDrink.recipientLobbyMemberId = drink.recipientLobbyMemberId;
        },
        setLobbyEvents(state, events) {
            state.lobbyEvents = events;
        },
        addLobbyEvent(state, event) {
            state.lobbyEvents.push(event);
        },
        changeName(state, newName) {
            const member = state.lobby.members.find(m => m.userId === state.currentUserId);
            if (!member)
                return;
            member.name = newName;
        },
        removeLobbyMember(state, lobbyMemberId) {
            state.lobby.members = state.lobby.members.filter(lm => lm.id != lobbyMemberId);
        },
        removePick(state, pickId) {
            state.lobby.members.forEach(m => m.picks = m.picks.filter(p => p.id != pickId));
        },
        addBot(state, args) {
            state.lobby.members.push({
                id: "",
                botPickStyle: args.botPickStyle,
                isBot: true,
                name: args.name,
                picks: []
            });
        }
    },
    actions: {
        async getLobby({ commit }, joinCode) {
            const lobby = await LobbyService.getLobbyByCode(joinCode);
            lobby.members.sort((a, b) => Number(a.joined) - Number(b.joined));
            commit('setLobby', lobby);
        },
        async getLobbyEvents({ commit }, lobbyId) {
            const lobbyEvents = await LobbyService.getLobbyEventsById(lobbyId);
            commit('setLobbyEvents', lobbyEvents);
        },
        async pickPlayer({ state, commit }, { gamePk, playerId, teamId, lobbyMemberId }) {
            const pick = {
                id: "",
                lobbyMemberId: lobbyMemberId,
                playerId: playerId,
                gamePk: gamePk,
                teamId: teamId,
                drinks: [],
                created: new Date(),
                isActive: true
            };
            commit('addPick', pick);
            const realPick = await LobbyService.makePick(state.lobby.joinCode, gamePk, playerId, teamId, lobbyMemberId);
            commit('updatePick', realPick);
        },
        async assignDrink({ state, commit }, args) {
            const drink = LobbyService.assignDrink(state.lobby.joinCode, args.drink.id, args.recipient.id);
            commit('assignDrink', drink);
        },
        async changeName({ state, commit }, newName) {
            commit('changeName', newName);
            await LobbyService.changeName(state.lobby?.joinCode, newName);
        },
        async removeLobbyMember({ state, commit }, lobbyMemberId) {
            commit('removeLobbyMember', lobbyMemberId);
            await LobbyService.removeLobbyMember(state.lobby?.joinCode, lobbyMemberId);
        },
        async removePick({ state, commit }, pickId) {
            commit('removePick', pickId);
            await LobbyService.removePick(state.lobby?.joinCode, pickId);
        },
        async addBot({ state, commit }, args) {
            commit('addBot', args);
            await LobbyService.joinLobbyByCode(state.lobby?.joinCode, args.name, true, args.botPickStyle);
        }
    },
    getters: {
        isLobbyAdmin(state) {
            return state.currentUserId === state.lobby?.createdBy;
        }
    }
};
//# sourceMappingURL=lobby.js.map