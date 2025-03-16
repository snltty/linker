<template>
    <el-form-item :label="$t('server.relaySecretKey')">
        <div >
            <div class="flex">
                <el-input class="flex-1" type="password" show-password v-model="state.list.SecretKey" maxlength="36" @change="handleSave" />
                <Sync class="mgl-1" name="RelaySecretKey"></Sync>
            </div>
            <div class="flex">
                <div class="mgr-1">
                    <el-checkbox class="mgr-1" v-model="state.list.SSL" :label="$t('server.relaySSL')" @change="handleSave" />
                    <el-checkbox v-model="state.list.Disabled" :label="$t('server.relayDisable')" @change="handleSave" />
                    
                </div>
                <a href="javascript:;" @click="state.show=true" class="mgl-1 delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
                    {{$t('server.relayNodes')}} : {{state.nodes.length}}
                </a>

                <div class="mgl-1">
                    <el-checkbox v-model="state.list.UseCdkey" :label="$t('server.relayUseCdkey')" @change="handleSave" />
                </div>
                <RelayCdkey></RelayCdkey>
            </div>
        </div>
       
    </el-form-item>   
    <el-dialog v-model="state.show" :title="$t('server.relayTitle')" width="760" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="500">
                <el-table-column property="Name" :label="$t('server.relayName')">
                    <template #default="scope">
                        <div>
                            <a :href="scope.row.Url" class="a-line blue" target="_blank">{{ scope.row.Name }}</a>
                            <a v-if="state.hasRelayCdkey" href="javascript:;" class="a-line a-edit" @click="handleEdit(scope.row)">
                                <span><el-icon><Edit /></el-icon></span>
                                <span v-if="(scope.row.AllowProtocol & 1) == 1">,tcp</span>
                                <span v-if="(scope.row.AllowProtocol & 2) == 2">,udp</span>
                            </a>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="MaxGbTotal" :label="$t('server.relayFlow')" width="160">
                    <template #default="scope">
                        <span v-if="scope.row.MaxGbTotal == 0">--</span>
                        <span v-else>{{ (scope.row.MaxGbTotalLastBytes/1024/1024/1024).toFixed(2) }}GB / {{ scope.row.MaxGbTotal }}GB</span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidth" :label="$t('server.relaySpeed')" width="80">
                    <template #default="scope">
                        <span v-if="scope.row.MaxBandwidth == 0">--</span>
                        <span v-else>{{ scope.row.MaxBandwidth }}Mbps</span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidthTotal" :label="`${$t('server.relaySpeed2')}/${$t('server.relaySpeed1')}`" width="120">
                    <template #default="scope">
                        <span>
                            <span>{{scope.row.BandwidthRatio}}Mbps</span>
                            <span>/</span>
                            <span v-if="scope.row.MaxBandwidthTotal == 0">--</span>
                            <span v-else>{{ scope.row.MaxBandwidthTotal }}Mbps</span>
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionRatio" :label="$t('server.relayConnection')" width="100">
                    <template #default="scope">
                        <span><strong>{{scope.row.ConnectionRatio}}</strong>/{{scope.row.MaxConnection}}</span>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" :label="$t('server.relayDelay')" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Public" :label="$t('server.relayPublic')" width="60">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Public " size="small" />
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
    <EditNode v-if="state.showEdit" v-model="state.showEdit" :data="state.current"></EditNode>
</template>
<script>
import { relayCdkeyAccess, setRelayServers, setRelaySubscribe } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, onUnmounted, reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
import RelayCdkey from './relayCdkey/Index.vue'
import EditNode from './EditNode.vue';
import { Edit } from '@element-plus/icons-vue';
export default {
    components:{Sync,RelayCdkey,EditNode,Edit},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Relay.Server,
            show:false,
            nodes:[],
            timer:0,
            showEdit:false,
            current:{},
            hasRelayCdkey:false
        });
        watch(()=>globalData.value.config.Client.Relay.Server,()=>{
            state.list.Delay = globalData.value.config.Client.Relay.Server.Delay;
        });

        const handleEdit = (row)=>{
            state.current = row;
            state.showEdit = true;
        }

        const handleSave = ()=>{
            setRelayServers(state.list).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });;
        }
        const _setRelaySubscribe = ()=>{
            setRelaySubscribe().then((res)=>{
                state.nodes = res;
                state.timer = setTimeout(_setRelaySubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            });
        }
        onMounted(()=>{
            _setRelaySubscribe();
            relayCdkeyAccess().then(res=>{
                state.hasRelayCdkey = res;
            }).catch(()=>{})
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        })

        return {state,handleSave,handleEdit}
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