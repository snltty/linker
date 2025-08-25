<template>
    <a @click="state.showMy = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.myCdkey')}}</a>
    <a v-if="state.super && hasCdkey" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.cdkey')}}</a>
    <Manager :type="state.type" v-if="state.showManager" v-model="state.showManager" :prefix="state.prefix" />
    <My :type="state.type" v-if="state.showMy" v-model="state.showMy" :prefix="state.prefix" />
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
import My from './My.vue'
export default {
    props:['type','prefix'],
    components:{Manager,My},
    setup (props) {

        const globalData = injectGlobalData();
        const hasCdkey = computed(()=>globalData.value.hasAccess('Cdkey')); 
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            showManager:false,
            showMy:false,
            type:props.type ,
            prefix:props.prefix ,
        });

        return {state,hasCdkey}
    }
}
</script>

<style lang="scss" scoped>

</style>