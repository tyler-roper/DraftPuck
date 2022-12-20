import Vue from 'vue';
import Router from 'vue-router'

Vue.use(Router);

const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('@/views/Home.vue')
    },
    {
        path: '/lobby/:id',
        name: 'Lobby',
        props: true,
        component: () => import('@/views/Lobby.vue')
    }
];

const router = new Router({
    mode: 'history',
    routes
});

export default router;