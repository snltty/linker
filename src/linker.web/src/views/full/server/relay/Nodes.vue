<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.relayTitle')" width="98%" top="2vh">
            <div>
                <el-table :data="state.nodes" size="small" border height="500">
                    <el-table-column property="Name" :label="$t('server.relayName')">
                        <template #default="scope">
                            <div> 
                                <a :href="scope.row.Url" class="a-line" :class="{green:scope.row.Public}" target="_blank" :title="scope.row.Public?$t('server.public'):''">{{ scope.row.Name }}</a>
                                <a v-if="state.super" href="javascript:;" class="a-line a-edit" @click="handleEdit(scope.row)">
                                    <span><el-icon><Edit /></el-icon></span>
                                    <span :class="{green:state.syncData.Value == 1 && scope.row.Id==state.syncData.Key}" 
                                    :title="state.syncData.Value == 1 && scope.row.Id==state.syncData.Key ? `${$t('server.relayDefault')}TCP`:''" 
                                    v-if="(scope.row.AllowProtocol & 1) == 1">,tcp</span>
                                    <span 
                                    :class="{green:state.syncData.Value == 2 && scope.row.Id==state.syncData.Key}" 
                                    :title="state.syncData.Value == 2 && scope.row.Id==state.syncData.Key ? `${$t('server.relayDefault')}UDP`:''" 
                                    v-if="(scope.row.AllowProtocol & 2) == 2">,udp</span>
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
                                <span>{{scope.row.BandwidthRatio}}</span>
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
                    <el-table-column property="Public" :label="$t('server.relayDefault')" width="60">
                        <template #default="scope">
                            <el-dropdown size="small">
                                <div class="dropdown">
                                    <span style="font-size: 1.2rem;">{{ $t('server.relayDefault') }}</span>
                                    <el-icon class="el-icon--right">
                                        <ArrowDown />
                                    </el-icon>
                                </div>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item v-if="(scope.row.AllowProtocol & 1) == 1" @click="handleShowSync(scope.row.Id, 1)">{{$t('common.relay')}}TCP</el-dropdown-item>
                                        <el-dropdown-item v-if="(scope.row.AllowProtocol & 2) == 2" @click="handleShowSync(scope.row.Id, 2)">{{$t('common.relay')}}UDP</el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
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
import {  getDefault,syncDefault } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, onUnmounted,  reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import Ids from '../../sync/Ids.vue';
import EditNode from './EditNode.vue';
import { Edit,ArrowDown } from '@element-plus/icons-vue';

export default {
    props: ['modelValue','data'],
    emits: ['update:modelValue','success'],
    components:{Ids,EditNode,Edit,ArrowDown},
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            show:true,
            nodes:props.data,
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

        onMounted(()=>{
            getDefault().then((res)=>{
                state.syncData.Key = res.Key || '';
                state.syncData.Value = res.Value || 0;
            });
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleEdit,domIds,handleShowSync,handleSync}
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