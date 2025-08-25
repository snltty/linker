<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.relayTitle')" width="98%" top="2vh">
            <div>
                <el-table :data="state.nodes" size="small" border height="500" stripe>
                    <el-table-column property="Name" :label="$t('server.relayName')">
                        <template #default="scope">
                            <div> 
                                <p class="flex">
                                    <a :href="scope.row.Url" class="a-line" 
                                    :class="{green:scope.row.Public}" target="_blank" 
                                    :title="scope.row.Public?$t('server.relayPublic'):''"><strong>{{ scope.row.Name }}</strong></a>
                                    <span class="flex-1"></span>
                                    <a href="javascript:;">
                                        <span v-if="(scope.row.AllowProtocol & 1) == 1">tcp</span>
                                        <span v-if="(scope.row.AllowProtocol & 2) == 2">,udp</span>
                                    </a>
                                </p>
                                <p class="flex">
                                    <el-checkbox v-if="state.super" class="mgr-p6" v-model="scope.row.Sync2Server" disabled size="small" @click="handleSync2Server(scope.row)">{{ $t('server.relaySync2Server') }}</el-checkbox>
                                    <template v-if="(scope.row.AllowProtocol & 1) == 1">
                                        <template v-if="state.syncData.Key == scope.row.Id && state.syncData.Value == 1">
                                            <el-checkbox class="mgr-p6" size="small" disabled checked>{{ $t('server.relayDefault') }}TCP</el-checkbox>
                                        </template>
                                        <template v-else>
                                            <el-checkbox class="mgr-p6" size="small" disabled @click.stop="handleShowSync(scope.row.Id, 1)">{{ $t('server.relayDefault') }}TCP</el-checkbox>
                                        </template>
                                    </template>
                                    <template v-if="(scope.row.AllowProtocol & 2) == 2">
                                        <template v-if="state.syncData.Key == scope.row.Id && state.syncData.Value == 2">
                                            <el-checkbox class="mgr-p6" size="small" disabled checked>{{ $t('server.relayDefault') }}UDP</el-checkbox>
                                        </template>
                                        <template v-else>
                                            <el-checkbox class="mgr-p6" size="small" disabled @click.stop="handleShowSync(scope.row.Id, 2)">{{ $t('server.relayDefault') }}UDP</el-checkbox>
                                        </template>
                                    </template>
                                    <span class="flex-1"></span>
                                    <a v-if="state.super" href="javascript:;" class="a-line a-edit" @click="handleUpdate(scope.row.Id)"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                    <a v-else href="javascript:;" class="a-line a-edit"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                </p>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column property="MaxGbTotal" :label="$t('server.relayFlow')" width="100">
                        <template #default="scope">
                            <template v-if="scope.row.MaxGbTotal == 0">--</template>
                            <template v-else>
                                <p>{{ scope.row.MaxGbTotal }}GB</p>
                                <p><strong>{{ (scope.row.MaxGbTotalLastBytes/1024/1024/1024).toFixed(2) }}GB</strong></p>
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="MaxBandwidth" :label="$t('server.relaySpeed')" width="80">
                        <template #default="scope">
                            <p>
                                <span v-if="scope.row.MaxBandwidth == 0">--</span>
                                <span v-else>{{ scope.row.MaxBandwidth }}Mbps</span>
                            </p>
                            <p>{{ scope.row.Delay }}ms</p>
                        </template>
                    </el-table-column> 
                    <el-table-column property="MaxBandwidthTotal" :label="$t('server.relaySpeed1')" width="100">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.MaxBandwidthTotal == 0">--</template>
                                <template v-else>{{ scope.row.MaxBandwidthTotal }}Mbps</template>
                            </p>
                            <p><strong>{{scope.row.BandwidthRatio}}mbps</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column property="ConnectionRatio" :label="$t('server.relayConnection')" width="80">
                        <template #default="scope">
                            <p>{{scope.row.MaxConnection}}</p>
                            <p><strong>{{scope.row.ConnectionRatio}}</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column v-if="state.super" property="Public" :label="$t('server.relayOper')" width="60">
                        <template #default="scope">
                            <p>
                                <a href="javascript:;" class="a-line" @click="handleExit(scope.row.Id)"><el-icon><Refresh /></el-icon>{{ $t('server.relayExit') }}</a>
                            </p>
                            <p>
                                <a href="javascript:;" class="a-line" @click="handleEdit(scope.row)"><el-icon><Edit /></el-icon>{{ $t('server.relayEdit') }}</a>
                            </p>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </el-dialog>
        <EditNode v-if="state.showEdit" v-model="state.showEdit" :data="state.current"></EditNode>
        <el-dialog class="options-center" :title="$t('server.relaySetDefault')" destroy-on-close v-model="state.showSync" width="54rem" top="2vh">
            <div>
                <div class="t-c">{{ $t('server.relaySetDefaultText') }}</div>
                <Ids ref="domIds"></Ids>
                <div class="t-c w-100 mgt-1">
                    <el-button @click="state.showSync = false">{{$t('common.cancel')}}</el-button>
                    <el-button type="primary" @click="handleSync">{{$t('common.confirm')}}</el-button>
                </div>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import {  getDefault,relayEdit,relayExit,relayUpdate,syncDefault } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, onMounted, onUnmounted,  reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import Ids from '../sync/Ids.vue';
import EditNode from './EditNode.vue';
import { Edit,ArrowDown,Refresh } from '@element-plus/icons-vue';

export default {
    props: ['modelValue','data'],
    emits: ['update:modelValue','success'],
    components:{Ids,EditNode,Edit,ArrowDown,Refresh},
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            show:true,
            nodes:computed(()=>props.data),
            showEdit:false,
            current:{},

            showSync:false,
            syncData:{
                Key:'',
                Value:0
            },
            super:computed(()=>globalData.value.signin.Super)
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleEdit = (row)=>{
            state.current = row;
            state.showEdit = true;
        }
        const domIds = ref(null);
        const handleShowSync  = (id,proto)=>{
            state.syncData.Key = id;
            state.syncData.Value = proto;
            state.showSync = true;
        }
        const handleSync = ()=>{
            syncDefault({
                Ids:domIds.value.getIds(),
                Data:state.syncData
            }).then((res)=>{
                state.showSync = false;
                ElMessage.success(t('common.oper'));
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleSync2Server = (row)=>{
            row.Sync2Server = !row.Sync2Server;
            row.AllowTcp= (row.AllowProtocol & 1) == 1,
            row.AllowUdp = (row.AllowProtocol & 2) == 2,
            relayEdit(row).then(res => {
                ElMessage.success(t('common.oper'));
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleExit = (id)=>{
            ElMessageBox.confirm(t('server.relayExit'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayExit(id).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleUpdate = (id)=>{
            ElMessageBox.confirm(`${t('server.relayUpdate')} ${globalData.value.signin.Version}`,t('server.relayUpdate'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayUpdate({Key:id,Value:globalData.value.signin.Version}).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }

        onMounted(()=>{
            getDefault().then((res)=>{
                state.syncData.Key = res.Key || '';
                state.syncData.Value = res.Value || 0;
            });
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleEdit,domIds,handleShowSync,handleSync,handleExit,handleUpdate,handleSync2Server}
    }
}
</script>
<style lang="stylus" scoped>
.blue {
    color: #409EFF;
}
.el-checkbox{font-weight:100;}
a.a-edit{
    .el-icon {
        vertical-align middle
    }
}
</style>