<template>
    <a @click="state.showMy = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.myCdkey')}}</a>
    <a v-if="state.hasCdkey && hasCdkey" @click="state.showManager = true" href="javascript:;" class="mgr-1 a-line">{{$t('server.cdkey')}}</a>
    <Manager :type="state.type" v-if="state.showManager" v-model="state.showManager" />
    <My :type="state.type" v-if="state.showMy" v-model="state.showMy" />
</template>

<script>
import { cdkeyAccess } from '@/apis/cdkey';
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
            hasCdkey:false,
            showManager:false,
            showMy:false,
            type:props.type 
        });

        onMounted(()=>{
            cdkeyAccess().then(res=>{
                state.hasCdkey = res;
            }).catch(()=>{})
        })

        return {state,hasCdkey}
    }
}
</script>

<style lang="scss" scoped>

</style>