import Vue from 'vue';
import Vuex from 'vuex';
import games from '@/store/modules/games';

Vue.use(Vuex);

export default new Vuex.Store({
    modules: {
        games
    }
});