import NHL from "@/services/NhlApiService";
import TeamColors from '@/models/teamColorLookup';
export default {
    namespaced: true,
    state: () => ({
        date: null,
        lastFetched: null,
        games: []
    }),
    mutations: {
        setDate(state, date) {
            state.date = date;
        },
        setGames(state, games) {
            state.games = games;
        },
        setLastFetched(state, lastFetched) {
            state.lastFetched = lastFetched;
        }
    },
    actions: {
        async getGames({ state, commit, dispatch }, date) {
            if (date === state.date && state.date != null && state.lastFetched != null)
                return state.games;
            const schedule = date
                ? await NHL.getSchedule(date)
                : await NHL.getSchedule();
            commit('setDate', null);
            commit('setGames', []);
            if (!schedule?.dates?.length || !schedule.dates[0].games?.length)
                return;
            commit('setDate', schedule.dates[0].date);
            const games = [];
            for (const scheduleGame of schedule.dates[0].games) {
                if (!scheduleGame.gamePk)
                    continue;
                games.push(await dispatch('getGame', { gamePk: scheduleGame.gamePk }));
            }
            commit('setLastFetched', schedule.metaData.timeStamp);
            commit('setGames', games);
            return state.games;
        },
        async getGame(_, payload) {
            const game = payload.startTimeCode
                ? await NHL.getGameData(payload.gamePk, payload.startTimeCode)
                : await NHL.getGameData(payload.gamePk);
            //set colors
            const awayTeam = game?.gameData?.teams?.away;
            const homeTeam = game?.gameData?.teams?.home;
            if (awayTeam?.id) {
                awayTeam.colors = { primary: TeamColors[awayTeam.id] };
                awayTeam.logo = require(`@/assets/img/logos/${awayTeam.abbreviation}.png`);
            }
            if (homeTeam?.id) {
                homeTeam.colors = { primary: TeamColors[homeTeam.id] };
                homeTeam.logo = require(`@/assets/img/logos/${homeTeam.abbreviation}.png`);
            }
            //set periods
            const gamePeriods = game?.liveData?.linescore?.periods;
            if (gamePeriods)
                for (let i = gamePeriods.length + 1; i <= 3; i++)
                    gamePeriods.push({ num: i });
            return game;
        }
    }
};
//# sourceMappingURL=games.js.map