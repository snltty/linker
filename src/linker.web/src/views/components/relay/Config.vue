<template>
    <el-form-item :label="$t('server.relay')">
        <div class="flex">
            <a href="javascript:;" @click="state.showModes=true" class="mgr-1 delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
                {{$t('server.relayNodes')}} : {{state.nodes.length}}
            </a>
            <WhiteList type="Relay"></WhiteList>
            <Nodes v-if="state.showModes" v-model="state.showModes" :data="state.nodes"></Nodes>
            <Status type="Relay"></Status>
        </div>
    </el-form-item>
</template>
<script>
import {  setRelaySubscribe } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { onMounted, onUnmounted, provide, reactive, ref } from 'vue'
import Sync from '../sync/Index.vue'
import Nodes from './Nodes.vue';
import WhiteList from '../wlist/Index.vue';
import Status from '../wlist/Status.vue';

export default {
    components:{Sync,Nodes,WhiteList,Status},
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            showModes:false,
            nodes:[],
            timer:0
        });

        const nodes = ref([]);
        provide('nodes',nodes);
        const _setRelaySubscribe = ()=>{
            clearTimeout(state.timer);
            setRelaySubscribe().then((res)=>{
                res.forEach((item)=>{
                    item._online = item.LastTicks < 15000;
                    item._manager = item.Manageable && item._online;
                });
                const onlines = res.filter((item)=>item._online);
                const offlines = res.filter((item)=>!item._online).sort((a,b)=>a.LastTicks-b.LastTicks);
                const list = onlines.concat(offlines);

                state.nodes = list;
                nodes.value = list;
                state.timer = setTimeout(_setRelaySubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            });
        }

        onMounted(()=>{
            _setRelaySubscribe();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state}
    }
}
</script>
<style lang="stylus" scoped>
.blue {
    color: #409EFF;
}
a.a-edit{
    margin-left:1rem;
    .el-icon {
        vertical-align middle
    }
}
</style>