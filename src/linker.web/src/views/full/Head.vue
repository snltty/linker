<template>
    <div>
        <div class="head flex">
            <div class="logo">
                <router-link :to="{name:'FullIndex'}">
                    <img src="@/assets/logo.png" alt="">
                </router-link>
            </div>
            <div class="menu flex-1">
                <ul class="flex">
                    <li>
                        <router-link :to="{name:'FullIndex'}"><img src="@/assets/shouye.svg"/> {{$t('head.home')}}</router-link>
                    </li>
                    <li v-if="hasConfig">
                        <router-link :to="{name:'FullServers'}"><img src="@/assets/fuwuqi.svg"/> {{$t('head.server')}}</router-link>
                    </li>
                    <li v-if="hasTransport">
                        <router-link :to="{name:'FullTransport'}"><img src="@/assets/dadong.svg"/> {{$t('head.protocol')}}</router-link>
                    </li>
                    <li v-if="hasAction">
                        <router-link :to="{name:'FullAction'}"><img src="@/assets/anquan.svg"/> {{$t('head.action')}}</router-link>
                    </li>
                    <li v-if="hasLogger">
                        <router-link :to="{name:'FullLogger'}"><img src="@/assets/rizhi.svg"/> {{$t('head.logger')}}</router-link>
                    </li>
                </ul>
            </div>
            <div class="locale">
                <el-dropdown>
                    <span class="el-dropdown-link">
                    {{localeOptions[locale]}}
                    <el-icon class="el-icon--right">
                        <arrow-down />
                    </el-icon>
                    </span>
                    <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item v-for="(item,index) in localeOptions" @click="handleLocale(index)">{{item}}</el-dropdown-item>
                    </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
            <div class="image">
                <Background name="full"></Background>
            </div>
        </div>
    </div>
</template>

<script>
import {Promotion,StarFilled,WarnTriangleFilled,PhoneFilled,HelpFilled,ArrowDown} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed, ref} from 'vue';
import Background from './Background.vue';
import { LOCALE_OPTIONS } from '@/lang'
import useLocale from '@/lang/provide'
export default {
    components:{Promotion,StarFilled,WarnTriangleFilled,PhoneFilled,HelpFilled,Background,ArrowDown},
    setup() {

        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config') || globalData.value.hasAccess('Sync') || globalData.value.hasAccess('Group')); 
        const hasLogger = computed(()=>globalData.value.hasAccess('LoggerShow')); 
        const hasTransport = computed(()=>globalData.value.hasAccess('Transport')); 
        const hasAction = computed(()=>globalData.value.hasAccess('Action')); 

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
        
        return {
            hasConfig,
            hasLogger,hasTransport,hasAction,localeOptions,locale,handleLocale
        }
    }
}
</script>

<style lang="stylus" scoped>
#file-input{opacity :0;position absolute;z-index :-1}
.head{
    background-color:#f6f8fa;
    border-bottom:1px solid #d0d7de;
    box-shadow:1px 1px 4px rgba(0,0,0,0.05);
    height:5rem;
    line-height:5rem;
    .logo{
        padding:.5rem 0 0 1rem;
        img{vertical-align:top;height:4rem;}
    }
    .menu{
        padding-left:1rem;font-size:1.4rem;
        li{box-sizing:border-box;padding:.5rem 0;margin-right:.5rem;}
        a{
            display:block;
            color:#333;
            padding:0 1rem;
            line-height:4rem
            &:hover,&.router-link-active{
                background-color:rgba(0,0,0,0.1);
                font-weight:bold;
                border-radius:4px;
            }

            img{
                height:2rem
                vertical-align:text-top;
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
    background-position:center bottom;

    position:absolute;
    left:0;
    top:0;
    right:0;
    bottom:0;
}
body.sunny .app-wrap{
    background-color:rgba(255,255,255,0.5);
}
body.sunny .status-wrap{
    background-color:rgba(245,245,245,0.3);
}
body.sunny .status-wrap .copy a{
    color:#333;
}
body.sunny .el-table{
    background-color:rgba(255,255,255,0.5);
}
body.sunny .head{
    background-color:rgba(246 248 250,0.5);
}
body.sunny .el-table tr{
    background-color:rgba(246 248 250,0.2);
}
body.sunny .el-table--striped .el-table__body tr.el-table__row--striped td.el-table__cell{
    background-color:rgba(246 248 250,0.2);
}
body.sunny .el-pagination__sizes, .el-pagination__total{
    color:#000;
}
body.sunny .status-wrap .copy a{
    color:#000;
}
body.sunny a{
    color:#576acf;
}
</style>