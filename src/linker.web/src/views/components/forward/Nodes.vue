<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.sforwardTitle')" width="98%" top="2vh">
            <div>
                <AccessShow value="ImportSForwardNode">
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
                    <el-table-column property="Name" :label="$t('server.sforwardName')" width="240">
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
                                            <a :href="scope.row.Url" class="a-line"  target="_blank">
                                                <strong>{{ scope.row.Name }}</strong>  
                                             </a>
                                        </el-badge>
                                    </p>
                                    <p class="flex">
                                        <span>{{ scope.row.Host }}</span>
                                        <span class="flex-1"></span>
                                        <AccessBoolean value="UpgradeSForwardNode">
                                            <template #default="{values}">
                                                <a v-if="state.super && values.UpgradeSForwardNode" href="javascript:;" class="a-line a-edit" @click="handleUpgrade(scope.row)"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                                <a v-else href="javascript:;" class="a-line a-edit"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                            </template>
                                        </AccessBoolean>
                                    </p>
                                </div>
                            </div>
                        </template>
                    </el-table-column>
                    
                    <el-table-column property="ConnectionsRatio" :label="$t('server.sforwardConnection')" width="80">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.Connections == 0">--</template>
                                <template v-else>{{ scope.row.Connections }}</template>
                            </p>
                            <p><strong>{{scope.row.ConnectionsRatio}}</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column property="DataEachMonth" :label="$t('server.sforwardFlow')" width="100">
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
                    <el-table-column property="BandwidthEach" :label="$t('server.sforwardSpeed')" width="80">
                        <template #default="scope">
                            <p>
                                <span v-if="scope.row.BandwidthEach == 0">--</span>
                                <span v-else>{{ scope.row.BandwidthEach }}Mbps</span>
                            </p>
                            <p>{{ scope.row.Delay }}ms</p>
                        </template>
                    </el-table-column> 
                    <el-table-column property="Bandwidth" :label="$t('server.sforwardSpeed1')" width="100">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.Bandwidth == 0">--</template>
                                <template v-else>{{ scope.row.Bandwidth }}Mbps</template>
                            </p>
                            <p><strong>{{scope.row.BandwidthRatio}}Mbps</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column property="Port" :label="$t('server.sforwardPort')" width="120">
                        <template #default="scope">
                            <p>{{$t('server.sforwardWebPort')}} : {{ scope.row.WebPort }}</p>
                            <p>{{ scope.row.TunnelPorts}}</p>
                        </template>
                    </el-table-column>
                    <el-table-column property="Manageable" fixed="right" :label="$t('server.sforwardOper')" width="110">
                        <template #default="scope">
                            <p>
                            <AccessBoolean v-if="state.super" value="RemoveSForwardNode,UpdateSForwardNode,ShareSForwardNode,RebootSForwardNode">
                                <template #default="{values}">
                                    <p>
                                        <el-button v-if="scope.row._manager && values.RebootSForwardNode" type="warning" plain size="small" @click="handleExit(scope.row)"><el-icon><Refresh /></el-icon></el-button>
                                        <el-button v-if="values.UpdateSForwardNode" plain size="small" @click="handleEdit(scope.row)"><el-icon><Edit /></el-icon></el-button>
                                    </p>
                                    <p>
                                        <el-button v-if="values.RemoveSForwardNode" type="danger" plain size="small" @click="handleRemove(scope.row)"><el-icon><CircleClose /></el-icon></el-button>
                                        <el-button v-if="scope.row._manager && values.ShareSForwardNode" type="info" plain size="small" @click="handleShare(scope.row)"><el-icon><Share /></el-icon></el-button>
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
        <NodeDeny v-model="state.showDeny" v-if="state.showDeny" type="sforward" :data="state.current"></NodeDeny>
    </div>
</template>
<script>
import {  sforwardExit, sforwardImport, sforwardRemove, sforwardShare, sforwardUpgrade } from '@/apis/sforward';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed,  onUnmounted,  reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import EditNode from './EditNode.vue';
import { Edit,ArrowDown,Refresh,CircleClose,Plus,Share } from '@element-plus/icons-vue';
import NodeDeny from '../node/Index.vue'
export default {
    props: ['modelValue','data'],
    emits: ['update:modelValue','success'],
    components:{EditNode,Edit,ArrowDown,Refresh,CircleClose,Plus,Share,NodeDeny},
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            show:true,
            nodes:computed(()=>props.data),
            showEdit:false,
            current:{},

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

        const handleEdit = (row)=>{
            state.current = row;
            state.showEdit = true;
        }
        const handleExit = (row)=>{
            ElMessageBox.confirm(t('server.sforwardExit'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                sforwardExit(row.NodeId).then(res => {
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
            ElMessageBox.confirm(`${t('server.sforwardUpdate')} ${globalData.value.signin.Version}`,t('server.sforwardUpdate'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                sforwardUpgrade({Key:row.NodeId,Value:globalData.value.signin.Version}).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleRemove = (row)=>{
            ElMessageBox.confirm(t('server.sforwardRemove'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                sforwardRemove(row.NodeId).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                //ElMessage.error(t('common.operFail'));
            });
        }
        const handleImport = ()=>{
            ElMessageBox.prompt(t('server.sforwardImport'), t('common.confirm'), {
                confirmButtonText:  t('common.confirm'),
                cancelButtonText: t('common.cancel')
            }).then(({ value }) => {
                if(!value) return;
                sforwardImport(value).then((res)=>{ 
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
            sforwardShare(row.NodeId).then((res)=>{
                ElMessageBox.prompt(t('server.sforwardShare'), t('common.tips'), {
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

        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleEdit,handleExit,handleUpgrade,handleRemove,handleImport,handleShare,handleDeny}
    }
}
</script>
<style lang="stylus" scoped>
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