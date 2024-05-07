import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Index',
        component: () => import('../views/devices/Index.vue')
    },
    {
        path: '/logger.html',
        name: 'Logger',
        component: () => import('../views/logger/Index.vue')
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
