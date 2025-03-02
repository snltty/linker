<template>
    <a href="javascript:;" class="mgl-1 a-line">{{$t('server.relayMyCdkey')}}</a>
    <a v-if="state.showManager && hasRelayCdkey" href="javascript:;" class="mgl-1 a-line">{{$t('server.relayCdkey')}}</a>
</template>

<script>
import { relayCdkeyAccess } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';

export default {
    setup () {

        const globalData = injectGlobalData();
        const hasRelayCdkey = computed(()=>globalData.value.hasAccess('RelayCdkey')); 
        const state = reactive({
            showManager:false
        });

        onMounted(()=>{
            relayCdkeyAccess().then(res=>{
                state.showManager = res;
            }).catch(()=>{})
        })

        return {state,hasRelayCdkey}
    }
}
</script>

<style lang="scss" scoped>

</style>