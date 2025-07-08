<template>
    <el-form-item :label="$t('server.sforward')">
        <div>
            <div class="flex">
                <!-- <Cdkey type="SForward"></Cdkey> -->
                <WhiteList type="SForward"></WhiteList>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { onUnmounted,  provide,  reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n';
import Cdkey from './cdkey/Index.vue'
import WhiteList from './wlist/Index.vue';

export default {
    components:{Cdkey,WhiteList},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
        });

        const nodes = ref([]);
        provide('nodes',nodes);
        const handleSave = ()=>{
        }
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
</style>