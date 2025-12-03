<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.relayTitle')" width="98%" top="2vh">
            <div>
                <el-table :data="state.nodes" size="small" border height="500" stripe>
                    <el-table-column property="Name" :label="$t('server.relayName')">
                        <template #default="scope">
                            <div class="flex"> 
                                <div>
                                    <a :href="scope.row.Url" target="_blank" >
                                        <img class="logo" :src="scope.row.Logo || 'https://linker.snltty.com/img/logo.png'" alt=""/>
                                    </a>
                                </div>
                                <div class="flex-1">
                                    <p class="flex">
                                        <a :href="scope.row.Url" class="a-line" 
                                        :class="{green:scope.row.Public}" target="_blank" 
                                        :title="scope.row.Public?$t('server.relayPublic'):''"><strong>{{ scope.row.Name }}</strong></a>
                                        <span class="flex-1"></span>
                                        <a href="javascript:;" class="protocol">
                                            <span v-if="(scope.row.Protocol & 1) == 1">tcp</span>
                                            <span v-if="(scope.row.Protocol & 2) == 2">,udp</span>
                                        </a>
                                        
                                    </p>
                                    <p class="flex">
                                        <div>
                                            <template v-if="state.syncData.Key == scope.row.Id">
                                                <el-checkbox size="small" disabled checked>{{ $t('server.relayDefault') }}</el-checkbox>
                                            </template>
                                            <template v-else>
                                                <el-checkbox size="small" disabled @click.stop="handleShowSync(scope.row, 1)">{{ $t('server.relayDefault') }}</el-checkbox>
                                            </template>
                                        </div>
                                        <span class="flex-1"></span>
                                        <a v-if="state.super" href="javascript:;" class="a-line a-edit" @click="handleUpdate(scope.row)"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                        <a v-else href="javascript:;" class="a-line a-edit"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                    </p>
                                </div>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column property="ConnectionsRatio" :label="$t('server.relayConnection')" width="80">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.Connections == 0">--</template>
                                <template v-else>{{ scope.row.Connections }}</template>
                            </p>
                            <p><strong>{{scope.row.ConnectionsRatio}}</strong></p>
                        </template>
                    </el-table-column>
                     <el-table-column property="Bandwidth" :label="$t('server.relaySpeed1')" width="100">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.Bandwidth == 0">--</template>
                                <template v-else>{{ scope.row.Bandwidth }}Mbps</template>
                            </p>
                            <p><strong>{{scope.row.BandwidthRatio}}Mbps</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column property="DataEachMonth" :label="$t('server.relayFlow')" width="100">
                        <template #default="scope">
                            <template v-if="scope.row.DataEachMonth == 0">
                                <p>--</p>
                                <p>--</p>
                            </template>
                            <template v-else>
                                <p>{{ scope.row.DataEachMonth }}GB</p>
                                <p><strong>{{ (scope.row.DataRemain/1024/1024/1024).toFixed(2) }}GB</strong></p>
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="BandwidthEach" :label="$t('server.relaySpeed')" width="80">
                        <template #default="scope">
                            <p>
                                <span v-if="scope.row.BandwidthEach == 0">--</span>
                                <span v-else>{{ scope.row.BandwidthEach }}Mbps</span>
                            </p>
                            <p>{{ scope.row.Delay }}ms</p>
                        </template>
                    </el-table-column> 
                    <el-table-column v-if="state.super" property="Manageable" :label="$t('server.relayOper')" width="110">
                        <template #default="scope">
                            <p>
                                <el-button v-if="scope.row.Manageable" size="small" @click="handleExit(scope.row)"><el-icon><Refresh /></el-icon></el-button>
                                <el-button size="small" @click="handleEdit(scope.row)"><el-icon><Edit /></el-icon></el-button>
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
                    <el-button @click="handleCancelSync">{{$t('common.cancel')}}</el-button>
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

        const _getDefault = ()=>{
            getDefault().then((res)=>{
                state.syncData.Key = res.Key || '';
                state.syncData.Value = res.Value || 0;
            });
        }

        const handleEdit = (row)=>{
            state.current = row;
            state.showEdit = true;
        }
        const domIds = ref(null);
        const handleShowSync  = (row,proto)=>{
            state.syncData.Key = row.Id;
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
                _getDefault();
            }).catch(()=>{
                _getDefault();
                //ElMessage.error(t('common.operFail'));
            });
        }
        const handleCancelSync = ()=>{
            state.showSync = false;
            _getDefault();
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
        const handleExit = (row)=>{
            ElMessageBox.confirm(t('server.relayExit'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayExit(row.Id).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleUpdate = (row)=>{
            if(row.Manageable == false) return;
            ElMessageBox.confirm(`${t('server.relayUpdate')} ${globalData.value.signin.Version}`,t('server.relayUpdate'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayUpdate({Key:row.Id,Value:globalData.value.signin.Version}).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }

        onMounted(()=>{
            _getDefault();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,
            handleEdit,domIds,handleShowSync,handleSync,handleCancelSync,
            handleExit,handleUpdate,handleSync2Server}
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
.logo{
    margin-right:1rem;
    height:4rem;
    vertical-align:text-top;
}
</style>