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
                meta: { title: 'head.home',access:'FullIndex',icon:'home.svg' },
                component: () => import('@/views/layout/full/list/Index.vue')
            },
            {
                path: '/full/servers.html',
                name: 'FullServers',
                meta: { title: 'head.server',access:'Config',icon:'server.svg' },
                component: () => import('@/views/layout/full/config/Index.vue')
            },
            {
                path: '/full/transport.html',
                name: 'FullTransport',
                meta: { title: 'head.protocol',access:'Transport',icon:'transport.svg' },
                component: () => import('@/views/layout/full/transport/Index.vue')
            },
            {
                path: '/full/action.html',
                name: 'FullAction',
                meta: { title: 'head.action',access:'Action',icon:'action.svg' },
                component: () => import('@/views/layout/full/action/Index.vue')
            },
            {
                path: '/full/firewall.html',
                name: 'FullFirewall',
                meta: { title: 'head.firewall',access:'FirewallSelf',icon:'firewall.svg' },
                component: () => import('@/views/layout/full/firewall/Index.vue')
            },
             {
                path: '/full/wakeup.html',
                name: 'FullWakeup',
                meta: { title: 'head.wakeup',access:'WakeupSelf',icon:'wakeup.svg' },
                component: () => import('@/views/layout/full/wakeup/Index.vue')
            },
            {
                path: '/full/logger.html',
                name: 'FullLogger',
                meta: { title: 'head.logger',access:'LoggerShow',icon:'logger.svg' },
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
