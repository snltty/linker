<template>
    <a v-if="state.hasWhiteList && hasWhiteList" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.wlist')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" :type="state.type" />
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
import { checkKey } from '@/apis/wlist';
export default {
    props:['type'],
    components:{Manager},
    setup (props) {

        const globalData = injectGlobalData();
        const hasWhiteList = computed(()=>globalData.value.hasAccess('WhiteList')); 
        const state = reactive({
            hasWhiteList:false,
            showManager:false,
            type:props.type
        });
        onMounted(()=>{
            checkKey().then(res=>{
                state.hasWhiteList = res;
            }).catch(()=>{})
        })

        return {state,hasWhiteList}
    }
}
</script>

<style lang="stylus" scoped>

</style>