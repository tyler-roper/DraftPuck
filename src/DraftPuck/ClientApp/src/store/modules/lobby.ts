import LobbyService from "@/services/LobbyService";
import { Commit } from 'vuex';

interface State {
    lobby: Lobby | null;
    lobbyEvents: Array<LobbyEvent>;
    currentUserId: string | null;
}

export default {
    namespaced: true,
    state: (): State => ({ lobby: null, lobbyEvents: [], currentUserId: null }),
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
        updatePick(state: State, pick: LobbyMemberPick) {
            const idx = state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks.findIndex(p => p.playerId === pick.playerId && pick.gamePk === p.gamePk);
            state.lobby.members.find(m => pick.lobbyMemberId === m.id).picks[idx] = pick;
        },
        assignDrink(state: State, drink: Drink) {
            const matchingDrink = state.lobby.members.flatMap(m => m.picks).flatMap(p => p.drinks).find(d => d.id === drink.id);

            if (matchingDrink)
                matchingDrink.recipientLobbyMemberId = drink.recipientLobbyMemberId;
        },
        setLobbyEvents(state: State, events: Array<LobbyEvent>) {
            state.lobbyEvents = events;
        },
        addLobbyEvent(state: State, event: LobbyEvent) {
            state.lobbyEvents.push(event);
        },
        changeName(state: State, newName: string) {
            const member = state.lobby.members.find(m => m.userId === state.currentUserId);
            if (!member) return;

            member.name = newName;
        }
    },
    actions: {
        async getLobby({ commit }: { commit: Commit }, joinCode: string) {
            const lobby = await LobbyService.getLobbyByCode(joinCode);
            lobby.members.sort((a, b) => Number(a.joined) - Number(b.joined));
            commit('setLobby', lobby);
        },
        async getLobbyEvents({ commit }: { commit: Commit }, lobbyId: string) {
            const lobbyEvents = await LobbyService.getLobbyEventsById(lobbyId);
            commit('setLobbyEvents', lobbyEvents);
        },
        async pickPlayer({ state, commit }: { state: State; commit: Commit }, { gamePk, playerId, teamId, lobbyMemberId }: { gamePk: number; playerId: number; teamId: number; lobbyMemberId: string | null }) {
            console.log(lobbyMemberId);
            const pick = {
                id: "",
                lobbyMemberId: lobbyMemberId,
                playerId: playerId,
                gamePk: gamePk,
                teamId: teamId,
                drinks: [],
                created: new Date()
            } as LobbyMemberPick
            commit('addPick', pick);

            const realPick = await LobbyService.makePick(state.lobby.joinCode, gamePk, playerId, teamId, lobbyMemberId);
            commit('updatePick', realPick);
        },
        async assignDrink({ state, commit }: { state: State; commit: Commit }, args: { drink: Drink; recipient: LobbyMember }) {
            const drink = LobbyService.assignDrink(state.lobby.joinCode, args.drink.id, args.recipient.id);
            commit('assignDrink', drink);
        },
        async changeName({ state, commit }: { state: State; commit: Commit }, newName: string) {
            commit('changeName', newName);
            await LobbyService.changeName(state.lobby?.joinCode, newName);
        }
    },
    getters: {
        isLobbyAdmin(state: State) {
            return state.currentUserId === state.lobby?.createdBy;
        }
    }
}