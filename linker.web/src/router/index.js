import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Full',
        component: () => import('@/views/full/Index.vue'),
        redirect: '/full/index.html',
        children: [
            {
                path: '/full/index.html',
                name: 'FullIndex',
                component: () => import('@/views/full/devices/List.vue')
            },
            {
                path: '/full/settings.html',
                name: 'FullSettings',
                component: () => import('@/views/full/settings/Index.vue')
            },
            {
                path: '/full/logger.html',
                name: 'FullLogger',
                component: () => import('@/views/full/logger/Index.vue')
            }
        ]
    },
    {
        path: '/net/index.html',
        name: 'Network',
        component: () => import('@/views/net/Index.vue')
    },
    {
        path: '/no-permission.html',
        name: 'NoPermission',
        component: () => import('@/views/NoPermission.vue')
    }

]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
