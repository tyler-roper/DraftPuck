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
            const pick = await LobbyService.makePick(state.lobby.joinCode, gamePk, playerId, teamId, lobbyMemberId);
            commit('addPick', pick);
        },
        async assignDrink({ state, commit }, args) {
            const drink = LobbyService.assignDrink(state.lobby.joinCode, args.drink.id, args.recipient.id);
            commit('assignDrink', drink);
        },
        async changeName({ state, commit }, newName) {
            commit('changeName', newName);
            await LobbyService.changeName(state.lobby?.joinCode, newName);
        }
    },
    getters: {
        isLobbyAdmin(state) {
            return state.currentUserId === state.lobby?.createdBy;
        }
    }
};
//# sourceMappingURL=lobby.js.map