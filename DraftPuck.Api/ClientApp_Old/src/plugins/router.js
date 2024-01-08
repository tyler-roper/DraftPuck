import Vue from 'vue';
import Router from 'vue-router';
Vue.use(Router);
const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('@/views/Home.vue'),
        meta: {
            title: "DRAFTPUCK - A live hockey drinking game.",
            metaTags: [
                { name: "title", content: "DRAFTPUCK - A live hockey drinking game." },
                { name: "description", content: "Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!" },
                { property: "og:type", content: "website" },
                { property: "og:url", content: "https://draftpuck.com/" },
                { property: "og:title", content: "DRAFTPUCK - A live hockey drinking game." },
                { property: "og:description", content: "Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!" },
                { property: "og:image", content: "/dist/assets/meta-img.png" },
                { property: "twitter:card", content: "summary_large_image" },
                { property: "twitter:url", content: "https://draftpuck.com/" },
                { property: "twitter:title", content: "DRAFTPUCK - A live hockey drinking game." },
                { property: "twitter:description", content: "Invite your friends, pick players, and give out drinks based on real-time events during live NHL games!" },
                { property: "twitter:image", content: "/dist/assets/meta-img.png" }
            ]
        }
    },
    {
        path: '/lobby/:joinCode',
        name: 'Lobby',
        props: true,
        component: () => import('@/views/Lobby.vue'),
        meta: {
            title: `DRAFTPUCK (Lobby: {{joinCode}})`,
            metaTags: [
                { name: "title", content: `DRAFTPUCK (Lobby: {{joinCode}})` },
                { property: "og:title", content: `DRAFTPUCK (Lobby: {{joinCode}})` },
                { property: "twitter:title", content: `DRAFTPUCK (Lobby: {{joinCode}})` }
            ]
        }
    }
];
const router = new Router({
    mode: 'history',
    routes
});
router.beforeEach((to, from, next) => {
    const metaTags = to.meta?.metaTags ?? [];
    metaTags.forEach(tag => {
        const firstKey = Object.keys(tag)[0];
        const element = document.querySelector(`meta[${firstKey}='${tag[firstKey]}']`);
        if (element) {
            let str = tag.content;
            Object.entries(to.params).forEach(([k, v]) => {
                str = str.replace(`{{${k}}}`, v);
            });
            element.setAttribute('content', str);
        }
    });
    if (to.meta?.title) {
        let str = to.meta.title;
        Object.entries(to.params).forEach(([k, v]) => {
            str = str.replace(`{{${k}}}`, v);
        });
        document.querySelector("title").innerHTML = str;
    }
    else {
        document.querySelector("title").innerHTML = "DRAFTPUCK - A live hockey drinking game.";
    }
    next();
});
export default router;
//# sourceMappingURL=router.js.map