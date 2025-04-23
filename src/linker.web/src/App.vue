<template>
    <div :class="{phone:globalData.isPhone}">
        <el-config-provider :locale="locale">
            <template v-if="configed">
                <router-view />
            </template>
            <Api></Api>
        </el-config-provider>
    </div>
</template>
<script>
import { computed } from 'vue';
import { provideGlobalData } from './provide';
import Api from './views/Api.vue';
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import en from 'element-plus/dist/locale/en.mjs'
import useLocale from './lang/provide';
export default {
    components:{Api},
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

</style>

