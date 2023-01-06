import Vue from 'vue';
import Router from 'vue-router';
Vue.use(Router);
const routes = [
    //{
    //    path: '/',
    //    name: 'Home',
    //    component: () => import('@/views/Home.vue')
    //},
    //{
    //    path: '/lobby/:code',
    //    name: 'Lobby',
    //    props: true,
    //    component: () => import('@/views/Lobby.vue')
    //},
    {
        path: '/',
        name: 'Lobby',
        component: () => import('@/views/Lobby.vue')
    }
];
const router = new Router({
    mode: 'history',
    routes
});
export default router;
//# sourceMappingURL=router.js.map