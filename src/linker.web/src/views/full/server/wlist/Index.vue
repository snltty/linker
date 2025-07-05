<template>
    <a v-if="hasWhiteList && state.super" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.wlist')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" :type="state.type" />
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
export default {
    props:['type'],
    components:{Manager},
    setup (props) {

        const globalData = injectGlobalData();
        const hasWhiteList = computed(()=>globalData.value.hasAccess('WhiteList')); 
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            showManager:false,
            type:props.type
        });
        return {state,hasWhiteList}
    }
}
</script>

<style lang="stylus" scoped>

</style>