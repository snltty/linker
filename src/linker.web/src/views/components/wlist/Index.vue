<template>
    <a v-if="state.super" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.wlist')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" :type="state.type" :prefix="state.prefix"/>
</template>

<script>
import { computed, reactive } from 'vue';
import Manager from './Manager.vue'
import { injectGlobalData } from '@/provide';
export default {
    props:['type','prefix'],
    components:{Manager},
    setup (props) {

        const globalData = injectGlobalData();
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            showManager:false,
            type:props.type,
            prefix:props.prefix
        });
        return {state}
    }
}
</script>

<style lang="stylus" scoped>

</style>