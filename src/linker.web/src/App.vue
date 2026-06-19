<template>
    <div class="app-inner absolute" :class="{phone:globalData.isPhone}">
        <el-config-provider :locale="locale">
            <router-view />
        </el-config-provider>
    </div>
    <Refresh></Refresh>
    <Api></Api>
</template>
<script>
import { computed} from 'vue';
import { provideGlobalData } from './provide';
import Api from './views/components/ws/Dialog.vue';
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
</style>

