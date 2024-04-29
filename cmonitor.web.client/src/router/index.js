import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Index',
        component: () => import('../views/Index.vue')
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
