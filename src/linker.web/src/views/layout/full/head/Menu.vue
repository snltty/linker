<template>
<div class="menu flex-1">
    <PcShow>
        <ul class="flex">
            <template v-for="item in routes">
                <AccessShow :value="item.meta.access">
                    <li>
                        <router-link :to="{name:item.name}"><img :src="item.meta.icon"/><span>{{$t(item.meta.title)}}</span></router-link>
                    </li>
                </AccessShow>
            </template>
        </ul>
    </PcShow>
    <PhoneShow>
        <ul class="flex">
            <template v-for="item in routes">
                <AccessShow :value="item.meta.access">
                    <li v-if="route.name == item.name">
                        <router-link :to="{name:item.name}"><img :src="item.meta.icon"/><span>{{$t(item.meta.title)}}</span></router-link>
                    </li>
                </AccessShow>
            </template>
            <li>
                <a href="javascript:void(0);" @click="refresh"><img src="refresh.svg"/><span>{{$t('head.refresh')}}</span></a>
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
                    <template v-for="item in routes">
                        <AccessShow :value="item.meta.access">
                            <el-dropdown-item>
                                <router-link :to="{name:item.name}"><img :src="item.meta.icon" height="20" style="vertical-align: text-top;"/><span>{{$t(item.meta.title)}}</span></router-link>
                            </el-dropdown-item>
                        </AccessShow>
                    </template>
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
import { useRoute, useRouter } from 'vue-router';
import { computed } from 'vue';
export default {
    components:{Background,Theme,Operation,ArrowDown},
    setup() {

        const route = useRoute();
        const router = useRouter();
        const routes = computed(()=>router.options.routes.filter(c=>c.name == 'Full')[0].children);

        const refresh = () => {
            window.location.reload();
        }

        return {
            route,routes,refresh
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