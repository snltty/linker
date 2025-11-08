<template>
    <div class="app-inner absolute" :class="{phone:globalData.isPhone}">
        <el-config-provider :locale="locale">
            <template v-if="configed">
                <AccessBoolean value="NetManager,FullManager">
                    <template #default="{values}">
                        <template v-if="($route.path.indexOf('/net/')== 0 && values.NetManager == false) || ($route.path.indexOf('/full/')== 0 && values.FullManager == false)">
                            <NoPermission></NoPermission>
                        </template>
                        <template v-else><router-view /></template>
                    </template>
                </AccessBoolean>
            </template>
            <Api></Api>
        </el-config-provider>
    </div>
    <Refresh></Refresh>
</template>
<script>
import { computed } from 'vue';
import { provideGlobalData } from './provide';
import Api from './views/Api.vue';
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import en from 'element-plus/dist/locale/en.mjs'
import useLocale from './lang/provide';
import Refresh from './views/Refresh.vue';
import NoPermission from './views/NoPermission.vue';
export default {
    components:{Api,Refresh,NoPermission},
    setup(props) {
        const globalData = provideGlobalData();
        const configed = computed(()=>globalData.value.config.configed);

        const {currentLocale} = useLocale();
        const locale = computed(() => (currentLocale.value == 'zh-CN' ? zhCn : en))

        return { configed,locale,globalData};
    }
}
</script>
<style lang="stylus" scoped>
html.dark .app-inner{
    background: radial-gradient(circle at 15% 50%, rgba(34, 197, 94, 0.4) 0px, transparent 0px) 0px 0px / 100% 100%, radial-gradient(circle at 85% 50%, rgba(22, 163, 74, 0.4) 0px, transparent 0px) 0px 0px / 100% 100%, linear-gradient(90deg, transparent 0%, rgba(34, 197, 94, 0.15) 15%, rgba(34, 197, 94, 0.25) 50%, rgba(22, 163, 74, 0.15) 85%, transparent 100%) 0px 50% / 100% 4px;
}
</style>

