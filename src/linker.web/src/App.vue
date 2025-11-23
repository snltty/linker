<template>
    <div class="app-inner absolute" :class="{phone:globalData.isPhone}">
        <el-config-provider :locale="locale">
            <router-view />
            <Api></Api>
        </el-config-provider>
    </div>
    <Refresh></Refresh>
</template>
<script>
import { computed} from 'vue';
import { provideGlobalData } from './provide';
import Api from './views/Api.vue';
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import en from 'element-plus/dist/locale/en.mjs'
import useLocale from './lang/provide';
import Refresh from './views/Refresh.vue';
export default {
    components:{Api,Refresh},
    setup(props) {
        const globalData = provideGlobalData();
        
        const {currentLocale} = useLocale();
        const locale = computed(() => (currentLocale.value == 'zh-CN' ? zhCn : en));

        return {locale,globalData};
    }
}
</script>
<style lang="stylus" scoped>
html.dark .app-inner{
    background: radial-gradient(circle at 15% 50%, rgba(34, 197, 94, 0.4) 0px, transparent 0px) 0px 0px / 100% 100%, radial-gradient(circle at 85% 50%, rgba(22, 163, 74, 0.4) 0px, transparent 0px) 0px 0px / 100% 100%, linear-gradient(90deg, transparent 0%, rgba(34, 197, 94, 0.15) 15%, rgba(34, 197, 94, 0.25) 50%, rgba(22, 163, 74, 0.15) 85%, transparent 100%) 0px 50% / 100% 4px;
}
</style>

