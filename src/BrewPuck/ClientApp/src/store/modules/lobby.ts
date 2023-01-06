import LobbyService from "@/services/LobbyService";
import { Commit } from 'vuex';

interface State {
    lobby: Lobby | null;
    currentUserId: string | null;
}

export default {
    namespaced: true,
    state: (): State => ({ lobby: null, currentUserId: null }),
    mutations: {
        setLobby(state: State, lobby: Lobby) {
            state.lobby = lobby;
        },
        setCurrentUserId(state: State, userId: string) {
            state.currentUserId = userId;
        },
        addPick(state: State, pick: LobbyMemberPick) {
            state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks.push(pick);
        },
        assignDrink(state: State, drink: Drink) {
            const matchingDrink = state.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(d => d.id === drink.id);

            if (matchingDrink)
                matchingDrink.recipientLobbyMemberId = drink.recipientLobbyMemberId;
        }
    },
    actions: {
        async getLobby({ commit }: { commit: Commit }, joinCode: string) {
            const lobby = await LobbyService.getLobbyByCode(joinCode);
            lobby.members.sort((a, b) => Number(a.joined) - Number(b.joined));
            commit('setLobby', lobby);
        },
        async pickPlayer({ state, commit }: { state: State; commit: Commit }, { gamePk, playerId, lobbyMemberId }: { gamePk: number; playerId: number; lobbyMemberId: string | null }) {
            const pick = await LobbyService.makePick(state.lobby.joinCode, gamePk, playerId, lobbyMemberId);
            commit('addPick', pick);
        },
        async assignDrink({ state, commit }: { state: State; commit: Commit }, args: { drink: Drink; recipient: LobbyMember }) {
            const drink = LobbyService.assignDrink(state.lobby.joinCode, args.drink.id, args.recipient.id);
            commit('assignDrink', drink);
        }
    },
    getters: {
        isLobbyAdmin(state: State) {
            return state.currentUserId === state.lobby?.createdBy;
        }
    }
}