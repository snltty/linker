<template>
    <el-form-item :label="$t('server.sforward')" v-if="state.super">
        <div>
            <div class="flex">
                <WhiteList type="SForward" v-if="hasWhiteList"></WhiteList>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { computed, onUnmounted,  provide,  reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n';
import Cdkey from '../cdkey/Index.vue'
import WhiteList from '../wlist/Index.vue';

export default {
    components:{Cdkey,WhiteList},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const hasWhiteList = computed(()=>globalData.value.hasAccess('WhiteList')); 
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            type:props.type
        });

        const nodes = ref([]);
        provide('nodes',nodes);
        const handleSave = ()=>{
        }
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,hasWhiteList,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
</style>