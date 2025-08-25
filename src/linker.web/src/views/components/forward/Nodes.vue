<template>
    <div>
        <el-dialog v-model="state.show" :title="$t('server.sforwardTitle')" width="98%" top="2vh">
            <div>
                <el-table :data="state.nodes" size="small" border height="500" stripe>
                    <el-table-column property="Name" :label="$t('server.sforwardName')">
                        <template #default="scope">
                            <div> 
                                <p class="flex">
                                    <a :href="scope.row.Url" class="a-line" :class="{green:scope.row.Public}" target="_blank"><strong>{{ scope.row.Name }}</strong></a>
                                    <span>({{ scope.row.Domain || scope.row.Address }})</span>
                                </p>
                                <p class="flex">
                                    <el-checkbox v-if="state.super" class="mgr-p6" v-model="scope.row.Sync2Server" disabled size="small" @click="handleSync2Server(scope.row)">{{ $t('server.sforwardSync2Server') }}</el-checkbox>
                                    <span class="flex-1"></span>
                                    <a v-if="state.super" href="javascript:;" class="a-line a-edit" @click="handleUpdate(scope.row.Id)"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                    <a v-else href="javascript:;" class="a-line a-edit"><el-icon><Refresh /></el-icon>{{ scope.row.Version }}</a>
                                </p>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column property="Port" :label="$t('server.sforwardPort')">
                        <template #default="scope">
                            <p>{{$t('server.sforwardWebPort')}} : {{ scope.row.WebPort }}</p>
                            <p>{{$t('server.sforwardPortRange')}} : {{ scope.row.PortRange[0]}} - {{ scope.row.PortRange[1]}}</p>
                        </template>
                    </el-table-column>
                    <el-table-column property="MaxGbTotal" :label="$t('server.sforwardFlow')" width="100">
                        <template #default="scope">
                            <template v-if="scope.row.MaxGbTotal == 0">--</template>
                            <template v-else>
                                <p>{{ scope.row.MaxGbTotal }}GB</p>
                                <p><strong>{{ (scope.row.MaxGbTotalLastBytes/1024/1024/1024).toFixed(2) }}GB</strong></p>
                            </template>
                        </template>
                    </el-table-column>
                    <el-table-column property="MaxBandwidth" :label="$t('server.sforwardSpeed')" width="80">
                        <template #default="scope">
                            <p>
                                <span v-if="scope.row.MaxBandwidth == 0">--</span>
                                <span v-else>{{ scope.row.MaxBandwidth }}Mbps</span>
                            </p>
                            <p>{{ scope.row.Delay }}ms</p>
                        </template>
                    </el-table-column> 
                    <el-table-column property="MaxBandwidthTotal" :label="$t('server.sforwardSpeed1')" width="100">
                        <template #default="scope">
                            <p>
                                <template v-if="scope.row.MaxBandwidthTotal == 0">--</template>
                                <template v-else>{{ scope.row.MaxBandwidthTotal }}Mbps</template>
                            </p>
                            <p><strong>{{scope.row.BandwidthRatio}}mbps</strong></p>
                        </template>
                    </el-table-column>
                    <el-table-column  v-if="state.super" property="Public" :label="$t('server.sforwardOper')" width="60">
                        <template #default="scope">
                            <p>
                                <a href="javascript:;" class="a-line" @click="handleExit(scope.row.Id)"><el-icon><Refresh /></el-icon>{{ $t('server.sforwardExit') }}</a>
                            </p>
                            <p>
                                <a href="javascript:;" class="a-line" @click="handleEdit(scope.row)"><el-icon><Edit /></el-icon>{{ $t('server.sforwardEdit') }}</a>
                            </p>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </el-dialog>
        <EditNode v-if="state.showEdit" v-model="state.showEdit" :data="state.current"></EditNode>
    </div>
</template>
<script>
import {  sforwardEdit,sforwardExit,sforwardUpdate } from '@/apis/sforward';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed,  onUnmounted,  reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import EditNode from './EditNode.vue';
import { Edit,ArrowDown,Refresh } from '@element-plus/icons-vue';

export default {
    props: ['modelValue','data'],
    emits: ['update:modelValue','success'],
    components:{EditNode,Edit,ArrowDown,Refresh},
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            show:true,
            nodes:computed(()=>props.data),
            showEdit:false,
            current:{},

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
        const handleSync2Server = (row)=>{
            row.Sync2Server = !row.Sync2Server;
            sforwardEdit(row).then(res => {
                ElMessage.success(t('common.oper'));
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleExit = (id)=>{
            ElMessageBox.confirm(t('server.sforwardExit'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                sforwardExit(id).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(() => {
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleUpdate = (id)=>{
            ElMessageBox.confirm(`${t('server.sforwardUpdate')} ${globalData.value.signin.Version}`,t('server.sforwardUpdate'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                sforwardUpdate({Key:id,Value:globalData.value.signin.Version}).then(res => {
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
            });
        }
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });

        return {globalData,state,handleEdit,handleExit,handleUpdate,handleSync2Server}
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
</style>