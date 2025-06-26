<template>
    <a v-if="state.hasUser2Node && hasUser2Node" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.relayUser2Node')}}</a>
    <Manager v-if="state.showManager" v-model="state.showManager" />
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, reactive } from 'vue';
import Manager from './Manager.vue'
export default {
    props:['keyState'],
    components:{Manager},
    setup (props) {

        const globalData = injectGlobalData();
        const hasUser2Node = computed(()=>globalData.value.hasAccess('User2Node')); 
        const state = reactive({
            hasUser2Node:computed(()=>props.keyState),
            showManager:false
        });

        return {state,hasUser2Node}
    }
}
</script>

<style lang="stylus" scoped>

</style>