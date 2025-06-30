<template>
    <el-form-item :label="$t('server.relaySecretKey')">
        <div >
            <div class="flex">
                <el-input :class="{success:state.keyState,error:state.keyState==false}" style="width:20rem;" type="password" show-password v-model="state.list.SecretKey" maxlength="36" @blur="handleSave" />
                <Sync class="mgl-1" name="RelaySecretKey"></Sync>
            </div>
            <div class="flex">
                <div class="mgr-1">
                    <el-checkbox class="mgr-1" v-model="state.list.SSL" :label="$t('server.relaySSL')" @change="handleSave" />
                    <el-checkbox v-model="state.list.Disabled" :label="$t('server.relayDisable')" @change="handleSave" />
                </div>
                <a href="javascript:;" @click="state.showModes=true" class="mgr-1 delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
                    {{$t('server.relayNodes')}} : {{state.nodes.length}}
                </a>
                <WhiteList type="Relay"></WhiteList>
                <div class="mgr-1" :title="$t('server.relayUseCdkeyTitle')">
                    <el-checkbox v-model="state.list.UseCdkey" :label="$t('server.relayUseCdkey')" @change="handleSave" />
                </div>
                <Cdkey type="Relay"></Cdkey>
                <Nodes v-if="state.showModes" v-model="state.showModes" :data="state.nodes" :keyState="state.keyState"></Nodes>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { checkRelayKey,   setRelayServers, setRelaySubscribe } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, onUnmounted, provide, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
import Cdkey from './cdkey/Index.vue'
import Nodes from './relay/Nodes.vue';
import WhiteList from './wlist/Index.vue';

export default {
    components:{Sync,Cdkey,Nodes,WhiteList},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Relay.Server,
            showModes:false,
            nodes:[],
            timer:0,
            keyState:false,
        });
        const handleSave = ()=>{
            setRelayServers(state.list).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
            handleCheckKey();
        }

        const nodes = ref([]);
        provide('nodes',nodes);
        const _setRelaySubscribe = ()=>{
            clearTimeout(state.timer);
            setRelaySubscribe().then((res)=>{
                state.nodes = res;
                nodes.value = res;
                state.timer = setTimeout(_setRelaySubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            });
        }
        const handleCheckKey = ()=>{
            checkRelayKey().then((res)=>{
                state.keyState = res;
            }).catch(()=>{});
        }

        onMounted(()=>{
            _setRelaySubscribe();
            handleCheckKey();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleSave}
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