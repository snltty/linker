<template>
<div class="menu flex-1">
    <PcShow>
        <ul class="flex">
            <li>
                <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg"/><span>{{$t('head.home')}}</span></router-link>
            </li>
            <AccessShow value="Config">
                <li>
                    <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"/><span>{{$t('head.server')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="Transport">
                <li>
                    <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"/><span>{{$t('head.protocol')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="Action">
                <li>
                    <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"/><span>{{$t('head.action')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="FirewallSelf">
                <li>
                    <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"/><span>{{$t('head.firewall')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="WakeupSelf">
                <li>
                    <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"/><span>{{$t('head.wakeup')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="LoggerShow">
                <li>
                    <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"/><span>{{$t('head.logger')}}</span></router-link>
                </li>
            </AccessShow>
        </ul>
    </PcShow>
    <PhoneShow>
        <ul class="flex">
            <li v-if="route.name == 'FullIndex'">
                <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg"/><span>{{$t('head.home')}}</span></router-link>
            </li>
            <AccessShow value="Config">
                <li v-if="route.name == 'FullServers'">
                    <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"/><span>{{$t('head.server')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="Transport">
                <li v-if="route.name == 'FullTransport'">
                    <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"/><span>{{$t('head.protocol')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="Action">
                <li v-if="route.name == 'FullAction'">
                    <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"/><span>{{$t('head.action')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="FirewallSelf">
                <li v-if="route.name == 'FullFirewall'">
                    <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"/><span>{{$t('head.firewall')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="WakeupSelf">
                <li v-if="route.name == 'FullWakeup'">
                    <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"/><span>{{$t('head.wakeup')}}</span></router-link>
                </li>
            </AccessShow>
            <AccessShow value="LoggerShow">
                <li v-if="route.name == 'FullLogger'">
                    <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"/> <span>{{$t('head.logger')}}</span></router-link>
                </li>
            </AccessShow>
            <li>
                <a href="javascript:void(0);" @click="refresh"><img src="@/assets/shuaxin2.svg"/><span>{{$t('head.refresh')}}</span></a>
            </li>
        </ul>
    </PhoneShow>
</div>
<PhoneShow>
    <div class="select">
        <el-dropdown>
            <span class="el-dropdown-link"><el-icon><Operation /></el-icon></span>
            <template #dropdown>
                <el-dropdown-menu class="select-menu">
                    <el-dropdown-item>
                        <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg" height="20" style="vertical-align: text-top;"/> {{$t('head.home')}}</router-link>
                    </el-dropdown-item>
                    <AccessShow value="Config">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.server')}}</router-link>
                        </el-dropdown-item>
                    </AccessShow>
                    <AccessShow value="Transport">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.protocol')}}</router-link>
                        </el-dropdown-item>
                    </AccessShow>
                    <AccessShow value="Action">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullAction'}"><img src="@/assets/login.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.action')}}</router-link>   
                        </el-dropdown-item>
                    </AccessShow>
                    <AccessShow value="FirewallSelf">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullFirewall'}"><img src="@/assets/anquan.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.firewall')}}</router-link>
                        </el-dropdown-item>
                    </AccessShow>
                    <AccessShow value="WakeupSelf">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullWakeup'}"><img src="@/assets/qidong.svg"   height="20" style="vertical-align: text-top;"/> {{$t('head.wakeup')}}</router-link>
                        </el-dropdown-item>
                    </AccessShow>
                    <AccessShow value="LoggerShow">
                        <el-dropdown-item>
                            <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"  height="20" style="vertical-align: text-top;"/> {{$t('head.logger')}}</router-link>
                        </el-dropdown-item>
                    </AccessShow>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </div>
</PhoneShow>
</template>

<script>
import {Operation,ArrowDown} from '@element-plus/icons-vue'
import Background from './Background.vue';
import Theme from './Theme.vue';
import { useRoute } from 'vue-router';
export default {
    components:{Background,Theme,Operation,ArrowDown},
    setup() {

        const route = useRoute();
        const refresh = () => {
            window.location.reload();
        }

        return {
            route,refresh
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