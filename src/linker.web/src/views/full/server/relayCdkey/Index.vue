<template>
    <a href="javascript:;" class="mgl-1 a-line">{{$t('server.relayMyCdkey')}}</a>
    <a v-if="state.hasRelayCdkey && hasRelayCdkey" @click="state.showManager = true" href="javascript:;" class="mgl-1 a-line">{{$t('server.relayCdkey')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" />
</template>

<script>
import { relayCdkeyAccess } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
export default {
    components:{Manager},
    setup () {

        const globalData = injectGlobalData();
        const hasRelayCdkey = computed(()=>globalData.value.hasAccess('RelayCdkey')); 
        const state = reactive({
            hasRelayCdkey:false,
            showManager:false,
            showList:false,
        });

        onMounted(()=>{
            relayCdkeyAccess().then(res=>{
                state.hasRelayCdkey = res;
            }).catch(()=>{})
        })

        return {state,hasRelayCdkey}
    }
}
</script>

<style lang="scss" scoped>

</style>