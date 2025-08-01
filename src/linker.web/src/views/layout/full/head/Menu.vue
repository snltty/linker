<template>
<div class="menu flex-1">
    <ul class="flex" v-if="globalData.isPc">
        <li>
            <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg"/><span>{{$t('head.home')}}</span></router-link>
        </li>
        <li v-if="hasConfig">
            <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"/><span>{{$t('head.server')}}</span></router-link>
        </li>
        <li v-if="hasTransport">
            <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"/><span>{{$t('head.protocol')}}</span></router-link>
        </li>
        <li v-if="hasAction">
            <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"/><span>{{$t('head.action')}}</span></router-link>
        </li>
        <li v-if="hasFirewall">
            <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"/><span>{{$t('head.firewall')}}</span></router-link>
        </li>
        <li v-if="hasWakeupSelf">
            <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"/><span>{{$t('head.wakeup')}}</span></router-link>
        </li>
        <li v-if="hasLogger">
            <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"/><span>{{$t('head.logger')}}</span></router-link>
        </li>
    </ul>
    <ul class="flex" v-else>
        <li v-if="route.name == 'FullIndex'">
            <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg"/><span>{{$t('head.home')}}</span></router-link>
        </li>
        <li v-if="hasConfig && route.name == 'FullServers'">
            <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"/><span>{{$t('head.server')}}</span></router-link>
        </li>
        <li v-if="hasTransport && route.name == 'FullTransport'">
            <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"/><span>{{$t('head.protocol')}}</span></router-link>
        </li>
        <li v-if="hasAction && route.name == 'FullAction'">
            <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"/><span>{{$t('head.action')}}</span></router-link>
        </li>
            <li v-if="hasFirewall && route.name == 'FullFirewall'">
            <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"/><span>{{$t('head.firewall')}}</span></router-link>
        </li>
            <li v-if="hasWakeupSelf && route.name == 'FullWakeup'">
            <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"/><span>{{$t('head.wakeup')}}</span></router-link>
        </li>
        <li v-if="hasLogger && route.name == 'FullLogger'">
            <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"/> <span>{{$t('head.logger')}}</span></router-link>
        </li>
        <li>
            <a href="javascript:void(0);" @click="refresh"><img src="@/assets/shuaxin2.svg"/><span>{{$t('head.refresh')}}</span></a>
        </li>
    </ul>
</div>
<div class="select" v-if="globalData.isPhone">
    <el-dropdown>
        <span class="el-dropdown-link"><el-icon><Operation /></el-icon></span>
        <template #dropdown>
            <el-dropdown-menu class="select-menu">
                <el-dropdown-item>
                    <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg" height="20" style="vertical-align: text-top;"/> {{$t('head.home')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasConfig">
                    <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.server')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasTransport">
                    <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.protocol')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasAction">
                    <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.action')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasFirewall">
                    <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.firewall')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasWakeupSelf">
                    <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.wakeup')}}</router-link>
                </el-dropdown-item>
                <el-dropdown-item v-if="hasLogger">
                    <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.logger')}}</router-link>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
</div>
</template>

<script>
import {Operation,ArrowDown} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed} from 'vue';
import Background from './Background.vue';
import Theme from './Theme.vue';
import { useRoute } from 'vue-router';
export default {
    components:{Background,Theme,Operation,ArrowDown},
    setup() {

        const route = useRoute();
        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config')); 
        const hasLogger = computed(()=>globalData.value.hasAccess('LoggerShow')); 
        const hasTransport = computed(()=>globalData.value.hasAccess('Transport')); 
        const hasAction = computed(()=>globalData.value.hasAccess('Action')); 
        const hasFirewall = computed(()=>globalData.value.hasAccess('FirewallSelf'));
        const hasWakeupSelf = computed(()=>globalData.value.hasAccess('WakeupSelf'));
        const refresh = () => {
            window.location.reload();
        }

        return {
            route,globalData,hasConfig,
            hasLogger,hasTransport,hasAction,hasFirewall,hasWakeupSelf,refresh
        }
    }
}
</script>

<style lang="stylus" scoped>
html.dark .head .menu a{
    color:#ccc;
    &:hover,&.router-link-active{ background-color:rgba(0,0,0,0.5); }
}
.menu{
    padding-left:1rem;font-size:1.4rem;
    li{box-sizing:border-box;padding:.5rem 0;margin-right:.2rem;}
    a{
        display:block;
        color:#333;
        padding:0 1rem;
        line-height:4rem;
        height:4rem;
        &:hover,&.router-link-active{
            background-color:rgba(0,0,0,0.1);
            font-weight:bold;
            border-radius:4px;
        }

        img{
            height:2rem
            margin-right:.2rem;
            margin-top:1rem;
        } 
        span{
            vertical-align:top;
        }
    }
}
.select{
    padding-right:1rem;
    .el-dropdown{
        vertical-align:middle;
        .el-icon{
            vertical-align:bottom;
            font-size:2rem;
        }
    }
}


</style>