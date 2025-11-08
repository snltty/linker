import { createRouter, createWebHashHistory } from 'vue-router'
const routes = [
    {
        path: '/',
        name: 'Full',
        component: () => import('@/views/layout/full/Index.vue'),
        redirect: '/full/index.html',
        children: [
            {
                path: '/full/index.html',
                name: 'FullIndex',
                component: () => import('@/views/layout/full/list/Index.vue')
            },
            {
                path: '/full/servers.html',
                name: 'FullServers',
                component: () => import('@/views/layout/full/config/Index.vue')
            },
            {
                path: '/full/transport.html',
                name: 'FullTransport',
                component: () => import('@/views/layout/full/transport/Index.vue')
            },
            {
                path: '/full/action.html',
                name: 'FullAction',
                component: () => import('@/views/layout/full/action/Index.vue')
            },
            {
                path: '/full/firewall.html',
                name: 'FullFirewall',
                component: () => import('@/views/layout/full/firewall/Index.vue')
            },
             {
                path: '/full/wakeup.html',
                name: 'FullWakeup',
                component: () => import('@/views/layout/full/wakeup/Index.vue')
            },
            {
                path: '/full/logger.html',
                name: 'FullLogger',
                component: () => import('@/views/layout/full/logger/Index.vue')
            }
        ]
    },
    {
        path: '/net/index.html',
        name: 'Network',
        component: () => import('@/views/layout/net/Index.vue')
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes
})

export default router
