<template>
    <div>
        <div class="head flex">
            <div class="logo">
                <router-link :to="{name:'FullIndex'}">
                    <img src="@/assets/logo.png" alt="">
                </router-link>
            </div>
            <Menu></Menu>
            <Locale></Locale>
            <Background name="full"></Background>
            <Theme name="full"></Theme>
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
import Menu from './Menu.vue'
import Locale from './Locale.vue'
export default {
    components:{Background,Theme,Operation,ArrowDown,Menu,Locale},
    setup() {

        const globalData = injectGlobalData();
        

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
            globalData,
            localeOptions,locale,handleLocale,refresh
        }
    }
}
</script>

<style lang="stylus" scoped>
#file-input{opacity :0;position absolute;z-index :-1}
html.dark .head{background-color:#242526;border-color:#575c61;}
.head{
    background-color:#f6f8fa;
    border-bottom:1px solid #d0d7de;
    height:5rem;
    line-height:5rem;
    border-radius:.5rem .5rem 0 0;
    .logo{
        padding:.5rem 0 0 1rem;
        height:4.5rem
        img{vertical-align:top;height:4rem;}
    }
}

</style>
<style lang="stylus">
body.sunny{
    background-image:url(../../../../../public/bg.jpg);
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