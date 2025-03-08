<template>
    <a @click="state.showMy = true" href="javascript:;" class="mgl-1 a-line">{{$t('server.relayMyCdkey')}}</a>
    <a v-if="state.hasRelayCdkey && hasRelayCdkey" @click="state.showManager = true" href="javascript:;" class="mgl-1 a-line">{{$t('server.relayCdkey')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" />
    <My v-if="state.showMy" v-model="state.showMy" />
</template>

<script>
import { relayCdkeyAccess } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
import My from './My.vue'
export default {
    components:{Manager,My},
    setup () {

        const globalData = injectGlobalData();
        const hasRelayCdkey = computed(()=>globalData.value.hasAccess('RelayCdkey')); 
        const state = reactive({
            hasRelayCdkey:false,
            showManager:false,
            showMy:true
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