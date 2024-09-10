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
                        <router-link :to="{name:'FullIndex'}"><el-icon size="16"><StarFilled /></el-icon> 首页</router-link>
                    </li>
                    <li v-if="hasConfig">
                        <router-link :to="{name:'FullSettings'}"><el-icon size="16"><Tools /></el-icon> 配置</router-link>
                    </li>
                    <li v-if="hasLogger">
                        <router-link :to="{name:'FullLogger'}"><el-icon size="16"><WarnTriangleFilled /></el-icon> 日志</router-link>
                    </li>
                </ul>
            </div>
        </div>
    </div>
</template>

<script>
import {Tools,StarFilled,WarnTriangleFilled} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
export default {
    components:{Tools,StarFilled,WarnTriangleFilled},
    setup() {

        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config')); 
        const hasLogger = computed(()=>globalData.value.hasAccess('LoggerShow')); 

        return {
            hasConfig,
            hasLogger
        }
    }
}
</script>

<style lang="stylus" scoped>
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
            }

            .el-icon{
                vertical-align:sub;
            }
        }
    }
}

</style>