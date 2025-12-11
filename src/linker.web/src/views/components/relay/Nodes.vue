<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.relayTitle')" width="98%" top="2vh">
            <div>
                <AccessShow value="ImportRelayNode">
                    <div class="head mgb-1" v-if="state.super">
                        <div class="flex">
                            <span class="flex-1"></span>
                            <div>
                                <el-button type="success" size="small" @click="handleImport"><el-icon><Plus /></el-icon></el-button>
                            </div>
                            <span class="flex-1"></span>
                        </div>
                    </div>
                </AccessShow>
                <el-table :data="state.nodes" size="small" border height="500" stripe>
                    <el-table-column property="Name" :label="$t('server.relayName')">
                        <template #default="scope">
                            <div class="flex"> 
                                <div>
                                    <a :href="scope.row.Url" target="_blank" >
                                        <img class="logo" :class="{'gray': !scope.row._online}" :src="scope.row.Logo || 'https://linker.snltty.com/img/logo.png'" alt=""/>
                                    </a>
                                </div>
                                <div class="flex-1">
                                    <p class="flex">
                                        <el-badge @click="handleDeny(scope.row)" type="success" :value="scope.row.MasterCount" :offset="[20, 10]">
                                            <a :href="scope.row.Url" class="a-line" :class="{green:scope.row.Public}" target="_blank" >
                                                <strong>{{ scope.row.Name }}</strong>
                                            </a>  
                                        </el-badge>
                                        <span class="flex-1"></span>
                                        <a href="javascript:;" class="protocol">
                                            <span v-if="(scope.row.Protocol & 1) == 1">tcp</span>
                                            <span v-if="(scope.row.Protocol & 2) == 2">,udp</span>
                                        </a>
                                        
                                    </p>
                                    <p class="flex">
                                        <div>
                                            <template v-if="state.syncData.Key == scope.row.NodeId">
                                                <el-checkbox size="small" disabled checked>{{ scope.row.Host }}</el-checkbox>
                                            </template>
                                            <template v-else>
                                                <el-checkbox size="small" disabled @click.stop="handleShowSync(scope.row, 1)">{{ scope.row.Host }}</el-checkbox>
                                            </template>
                                        </div>
                                        <span class="flex-1"></span>
                                        <AccessBoolean value="UpgradeRelayNode">
                                            <template #default="{values}">
                                                <a v-if="state.super && values.UpgradeRelayNode" href="javascript:;" class="a-line a-edit" @click="handleUpgrade(scope.row)"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                                <a v-else href="javascript:;" class="a-line a-edit"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                            </template>
                                        </AccessBoolean>
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
                    <el-table-column  property="Manageable" :label="$t('server.relayOper')" width="110">
                        <template #default="scope">
                            <p>
                                <AccessBoolean v-if="state.super" value="RemoveRelayNode,UpdateRelayNode,ShareRelayNode,RebootRelayNode">
                                    <template #default="{values}">
                                        <p>
                                            <el-button v-if="scope.row._manager && values.RebootRelayNode" type="warning" plain size="small" @click="handleExit(scope.row)"><el-icon><Refresh /></el-icon></el-button>
                                            <el-button v-if="values.UpdateRelayNode" plain size="small" @click="handleEdit(scope.row)"><el-icon><Edit /></el-icon></el-button>
                                        </p>
                                        <p>
                                            <el-button v-if="values.RemoveRelayNode" type="danger" plain size="small" @click="handleRemove(scope.row)"><el-icon><CircleClose /></el-icon></el-button>
                                            <el-button v-if="scope.row._manager && values.ShareRelayNode " type="info" plain size="small" @click="handleShare(scope.row)"><el-icon><Share /></el-icon></el-button>
                                        </p>
                                    </template>
                                </AccessBoolean>
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
        <NodeDeny v-model="state.showDeny" v-if="state.showDeny" type="relay" :data="state.current"></NodeDeny>
    </div>
</template>
<script>
import {  getDefault,relayExit,relayImport,relayRemove,relayShare,relayUpgrade,syncDefault } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, onMounted, onUnmounted,  reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import Ids from '../sync/Ids.vue';
import EditNode from './EditNode.vue';
import { Edit,ArrowDown,Refresh,CircleClose,Plus,Share } from '@element-plus/icons-vue';
import NodeDeny from '../node/Index.vue'
export default {
    props: ['modelValue','data'],
    emits: ['update:modelValue','success'],
    components:{Ids,EditNode,Edit,ArrowDown,Refresh,CircleClose,Plus,Share,NodeDeny},
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
            super:computed(()=>globalData.value.signin.Super),

            showDeny:false
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
            state.syncData.Key = row.NodeId;
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

        const handleExit = (row)=>{
            ElMessageBox.confirm(t('server.relayExit'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayExit(row.NodeId).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                //ElMessage.error(t('common.operFail'));
            });
        }
        const handleUpgrade = (row)=>{
            if(row._manager == false) return;
            ElMessageBox.confirm(`${t('server.relayUpdate')} ${globalData.value.signin.Version}`,t('server.relayUpdate'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayUpgrade({Key:row.NodeId,Value:globalData.value.signin.Version}).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleRemove = (row)=>{
            ElMessageBox.confirm(t('server.relayRemove'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                relayRemove(row.NodeId).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                //ElMessage.error(t('common.operFail'));
            });
        }
        const handleImport = ()=>{
            ElMessageBox.prompt(t('server.relayImport'), t('common.confirm'), {
                confirmButtonText:  t('common.confirm'),
                cancelButtonText: t('common.cancel')
            }).then(({ value }) => {
                if(!value) return;
                relayImport(value).then((res)=>{ 
                    if(res){
                        ElMessage.error(res);
                    }else{
                        ElMessage.success(t('common.oper'));
                    }
                }).catch(()=>{})
            }).catch(() => {
            })
        }
        const handleShare = (row)=>{
            relayShare(row.NodeId).then((res)=>{
                ElMessageBox.prompt(t('server.relayShare'), t('common.tips'), {
                    confirmButtonText:  t('common.confirm'),
                    cancelButtonText: t('common.cancel'),
                    inputValue:res
                }).then(({ value }) => {
                    navigator.clipboard.writeText(value)
                }).catch(() => {
                })
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleDeny = (row)=>{
            state.current = row;
            state.showDeny = true;
        }

        onMounted(()=>{
            _getDefault();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,
            handleEdit,domIds,handleShowSync,handleSync,handleCancelSync,
            handleExit,handleUpgrade,handleRemove,handleImport,handleShare,handleDeny}
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
.gray{
    filter: grayscale(100%);
}
</style>