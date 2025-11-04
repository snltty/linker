<template>
    <el-form-item :label="$t('server.sforward')">
        <div>
            <div class="flex">
                <a href="javascript:;" @click="state.showModes = true" class="mgr-1 delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
                    {{$t('server.sforwardNodes')}} : {{state.nodes.length}}
                </a>
                <WhiteList type="SForward" prefix="sfp->"  v-if="state.super && hasWhiteList"></WhiteList>
                <Nodes v-if="state.showModes" v-model="state.showModes" :data="state.nodes"></Nodes>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, onUnmounted,  provide,  reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n';
import WhiteList from '../wlist/Index.vue';
import Nodes from './Nodes.vue';
import { setSForwardSubscribe } from '@/apis/sforward';

export default {
    components:{WhiteList,Nodes},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const hasWhiteList = computed(()=>globalData.value.hasAccess('WhiteList')); 
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            type:props.type,

            showModes:false,
            timer:0,
            nodes:[]
        });

        const nodes = ref([]);
        provide('nodes',nodes);
        const _setSForwardSubscribe = ()=>{
            clearTimeout(state.timer);
            setSForwardSubscribe().then((res)=>{
                state.nodes = res;
                nodes.value = res;
                state.timer = setTimeout(_setSForwardSubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setSForwardSubscribe,1000);
            });
        }
        onMounted(()=>{
            _setSForwardSubscribe();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,hasWhiteList}
    }
}
</script>
<style lang="stylus" scoped>
</style>