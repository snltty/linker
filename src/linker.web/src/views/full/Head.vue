<template>
    <div>
        <div class="head flex">
            <div class="logo">
                <router-link :to="{name:'FullIndex'}">
                    <img src="@/assets/logo.png" alt="">
                </router-link>
            </div>
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
            <div class="locale" v-if="globalData.isPc">
                <el-dropdown>
                    <span class="el-dropdown-link">
                    {{localeOptions[locale]}}
                    <el-icon>
                        <ArrowDown />
                    </el-icon>
                    </span>
                    <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item v-for="(item,index) in localeOptions" @click="handleLocale(index)">{{item}}</el-dropdown-item>
                    </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
            <div class="image" v-if="globalData.isPc">
                <Background name="full"></Background>
            </div>
            <div class="image" v-if="globalData.isPc">
                <Theme name="full"></Theme>
            </div>
        </div>
    </div>
</template>

<script>
import {Operation,ArrowDown} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed, ref} from 'vue';
import Background from './Background.vue';
import Theme from './Theme.vue';
import { LOCALE_OPTIONS } from '@/lang'
import useLocale from '@/lang/provide'
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
        const hasGroup = computed(()=>globalData.value.hasAccess('Group'));
        const hasFirewall = computed(()=>globalData.value.hasAccess('FirewallSelf'));
        const hasWakeupSelf = computed(()=>globalData.value.hasAccess('WakeupSelf'));
        

        const localeOptions = ref(LOCALE_OPTIONS);
        const { changeLocale, currentLocale } = useLocale()
        const locale = computed({
            get() {
                return currentLocale.value
            },
            set(value) {
                changeLocale(value)
            }
        });
        const handleLocale = (index) => {
            locale.value =index;
        }

        const refresh = () => {
            window.location.reload();
        }

        return {
            route,globalData,hasConfig,hasGroup,
            hasLogger,hasTransport,hasAction,hasFirewall,hasWakeupSelf,localeOptions,locale,handleLocale,refresh
        }
    }
}
</script>

<style lang="stylus" scoped>
#file-input{opacity :0;position absolute;z-index :-1}
html.dark .head{background-color:#242526;border-color:#575c61;}
html.dark .head .menu a{
    color:#ccc;
    &:hover,&.router-link-active{
        background-color:rgba(0,0,0,0.5);
    }
}
.head{
    background-color:#f6f8fa;
    border-bottom:1px solid #d0d7de;
    box-shadow:1px 1px 4px rgba(0,0,0,0.05);
    height:5rem;
    line-height:5rem;
    .logo{
        padding:.5rem 0 0 1rem;
        height:4.5rem
        img{vertical-align:top;height:4rem;}
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
    .locale{
        padding-right:1rem;
        .el-dropdown{
            vertical-align:middle;
            .el-icon{
                vertical-align:bottom;
            }
        }
    }
    .image{
        padding-right:1rem;
    }
}

</style>
<style lang="stylus">
body.sunny{
    background-image:url(../../../public/bg.jpg);
    background-repeat:no-repeat;
    background-size:cover;  
    background-position:center center;

    position:absolute;
    left:0;
    top:0;
    right:0;
    bottom:0;
}
body.sunny .app-wrap{ background-color:rgba(255,255,255,0.5);}
html.dark body.sunny .app-wrap{ background-color:rgba(0,0,0,0.5);}

body.sunny .status-wrap{ background-color:rgba(245,245,245,0.3);}
html.dark body.sunny .status-wrap{ background-color:rgba(0,0,0,0.3);}
html.dark body.sunny .flow-wrap{ background-color:rgba(0,0,0,0.3);}
body.sunny .status-wrap .copy a{
    color:#333;
}
body.sunny .el-table{ background-color:rgba(255,255,255,0.5);}
html.dark body.sunny .el-table{background-color:rgba(0,0,0,0.3);}
html.dark body.sunny .el-table th.el-table__cell{background-color:rgba(0,0,0,0.3);}

body.sunny .head{ background-color:rgba(246 248 250,0.5);}
html.dark body.sunny .head{background-color:rgba(0,0,0,0.2);}

body.sunny .el-table tr{ background-color:rgba(246 248 250,0.2);}
html.dark body.sunny .el-table tr{background-color:rgba(0,0,0,0.2);}

body.sunny .el-table--striped .el-table__body tr.el-table__row--striped td.el-table__cell{ background-color:rgba(246 248 250,0.2);}
html.dark body.sunny .el-table--striped .el-table__body tr.el-table__row--striped td.el-table__cell{ background-color:rgba(0,0,0,0.1);}

body.sunny .el-pagination__sizes,body.sunny  .el-pagination__total{ color:#000;}
html.dark body.sunny .el-pagination__sizes,body.sunny  .el-pagination__total{ color:#999;}
body.sunny .status-wrap .copy a{ color:#000;}

html.dark body.sunny .el-card,
html.dark body.sunny .el-tabs--border-card,
html.dark body.sunny .el-tabs--border-card>.el-tabs__header,
html.dark body.sunny .el-tabs--border-card>.el-tabs__header .el-tabs__item.is-active{background-color:rgba(0,0,0,0.3);}
</style>