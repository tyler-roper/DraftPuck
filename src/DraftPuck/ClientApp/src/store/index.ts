import Vue from 'vue';
import Vuex from 'vuex';
import lobby from '@/store/modules/lobby';

Vue.use(Vuex);

export default new Vuex.Store({
    modules: {
        lobby
    }
});