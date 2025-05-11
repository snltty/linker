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
                path: '/full/servers.html',
                name: 'FullServers',
                component: () => import('@/views/full/server/Index.vue')
            },
            {
                path: '/full/transport.html',
                name: 'FullTransport',
                component: () => import('@/views/full/transport/Index.vue')
            },
            {
                path: '/full/action.html',
                name: 'FullAction',
                component: () => import('@/views/full/action/Index.vue')
            },
            {
                path: '/full/firewall.html',
                name: 'FullFirewall',
                component: () => import('@/views/full/firewall/Index.vue')
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
