<template>
    <a @click="state.showMy = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.myCdkey')}}</a>
    <a v-if="state.super && hasCdkey" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.cdkey')}}</a>
    <Manager :type="state.type" v-if="state.showManager" v-model="state.showManager" />
    <My :type="state.type" v-if="state.showMy" v-model="state.showMy" />
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue';
import Manager from './Manager.vue'
import My from './My.vue'
export default {
    props:['type'],
    components:{Manager,My},
    setup (props) {

        const globalData = injectGlobalData();
        const hasCdkey = computed(()=>globalData.value.hasAccess('Cdkey')); 
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            showManager:false,
            showMy:false,
            type:props.type 
        });

        return {state,hasCdkey}
    }
}
</script>

<style lang="scss" scoped>

</style>