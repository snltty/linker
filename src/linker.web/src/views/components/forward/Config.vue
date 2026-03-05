<template>
    <el-form-item :label="$t('server.sforward')">
        <div class="flex">
            <a href="javascript:;" @click="state.showModes = true" class="mgr-1 delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
                {{$t('server.sforwardNodes')}} : {{state.nodes.length}}
            </a>
            <AccessShow value="WhiteList">
                <WhiteList type="SForward" prefix="sfp->"  v-if="state.super"></WhiteList>
            </AccessShow>
            <Nodes v-if="state.showModes" v-model="state.showModes" :data="state.nodes"></Nodes>
            <!-- <Status type="SForward"></Status> -->
        </div>
    </el-form-item>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { computed, onMounted, onUnmounted,  provide,  reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n';
import WhiteList from '../wlist/Index.vue';
import Nodes from './Nodes.vue';
import { sforwardSubscribe } from '@/apis/sforward';
import Status from '../wlist/Status.vue';

export default {
    components:{WhiteList,Nodes,Status},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            super:computed(()=>globalData.value.signin.Super),
            type:props.type,

            showModes:false,
            timer:0,
            nodes:[]
        });

        const nodes = ref([]);
        provide('nodes',nodes);
        const _sforwardSubscribe = ()=>{
            clearTimeout(state.timer);
            sforwardSubscribe().then((res)=>{
                res.forEach((item)=>{
                    item._online = item.LastTicks < 15000;
                    item._manager = item.Manageable && item._online;
                });
                const onlines = res.filter((item)=>item._online);
                const offlines = res.filter((item)=>!item._online).sort((a,b)=>a.LastTicks-b.LastTicks);
                const list = onlines.concat(offlines);

                state.nodes = list;
                nodes.value = list;
                state.timer = setTimeout(_sforwardSubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_sforwardSubscribe,1000);
            });
        }
        onMounted(()=>{
            _sforwardSubscribe();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state}
    }
}
</script>
<style lang="stylus" scoped>
</style>