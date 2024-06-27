<template>
  <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" :title="`端口转发到【${state.machineName}】`" top="1vh" width="600">
        <div>
            <div class="t-c head">
                <el-button type="success" size="small" @click="handleAdd">添加</el-button>
                <el-button size="small" @click="handleRefresh">刷新</el-button>
            </div>
            <el-table :data="state.data" size="small" border height="500" @cell-dblclick="handleCellClick">
                <el-table-column property="Name" label="名称">
                    <template #default="scope">
                        <template v-if="scope.row.NameEditing">
                            <el-input autofocus size="small" v-model="scope.row.Name"
                                @blur="handleEditBlur(scope.row, 'Name')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.Name }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="BindIPAddress" label="监听IP" width="140">
                    <template #default="scope">
                        <el-select v-model="scope.row.BindIPAddress" size="small">
                            <el-option v-for="item in state.ips" :key="item" :label="item" :value="item"/>
                        </el-select>
                    </template>
                </el-table-column>
                <el-table-column property="Port" label="监听端口" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.PortEditing">
                            <el-input type="number" autofocus size="small" v-model="scope.row.Port"
                                @blur="handleEditBlur(scope.row, 'Port')"></el-input>
                        </template>
                        <template v-else>
                            <template v-if="scope.row.Msg">
                                <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="scope.row.Msg">
                                    <template #reference>
                                        <div class="error">
                                            <span>{{ scope.row.Port }}</span>
                                            <el-icon size="20"><WarnTriangleFilled /></el-icon>
                                        </div>
                                    </template>
                                </el-popover>
                            </template>
                            <template v-else>
                                <span :class="{green:scope.row.Started}">{{ scope.row.Port }}</span>
                            </template>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="TargetEP" label="目标服务" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.TargetEPEditing">
                            <el-input autofocus size="small" v-model="scope.row.TargetEP"
                                @blur="handleEditBlur(scope.row, 'TargetEP')"></el-input>
                        </template>
                        <template v-else>
                            <template v-if="scope.row.TargetMsg">
                                <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="scope.row.TargetMsg">
                                    <template #reference>
                                        <span class="error">{{ scope.row.TargetEP }}</span>
                                        <el-icon size="20"><WarnTriangleFilled /></el-icon>
                                    </template>
                                </el-popover>
                            </template>
                            <template v-else>
                                <span :class="{green:scope.row.Started}">{{ scope.row.TargetEP }}</span>
                            </template>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="Started" label="状态" width="60">
                    <template #default="scope">
                        <el-switch v-model="scope.row.Started" @change="handleStartChange(scope.row)" inline-prompt
                            active-text="开" inactive-text="关" />
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="54">
                    <template #default="scope">
                        <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="删除不可逆，是否确认?"
                            @confirm="handleDel(scope.row.Id)">
                            <template #reference>
                                <el-button type="danger" size="small"><el-icon><Delete /></el-icon></el-button>
                            </template>
                        </el-popconfirm>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { inject, onMounted, onUnmounted, reactive, watch } from 'vue';
import { getForwardInfo, removeForwardInfo, addForwardInfo ,getForwardIpv4,testTargetForwardInfo,testListenForwardInfo } from '@/apis/forward'
import { ElMessage } from 'element-plus';
import {WarnTriangleFilled} from '@element-plus/icons-vue'
export default {
    props: ['data','modelValue'],
    emits: ['update:modelValue'],
    components:{WarnTriangleFilled},
    setup(props, { emit }) {

        const forward = inject('forward');
        const state = reactive({
            show: true,
            machineId: forward.value.current,
            machineName: forward.value.machineName,
            data: [],
            ips:[],
            timerTestTarget:0,
            timerTestListen:0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _getForwardIpv4 = ()=>{
            getForwardIpv4().then((res)=>{
                res.splice(0,0,'0.0.0.0');
                res.splice(1,0,'127.0.0.1');
                state.ips = res;
            }).catch(()=>{});
        }

        const _testTargetForwardInfo = ()=>{
            testTargetForwardInfo(forward.value.current).then((res)=>{
               state.timerTestTarget = setTimeout(_testTargetForwardInfo,1000);
            }).catch(()=>{
                state.timerTestTarget = setTimeout(_testTargetForwardInfo,1000);
            });
        }
        const _testListenForwardInfo = ()=>{
            testListenForwardInfo(forward.value.current).then((res)=>{
               state.timerTestListen = setTimeout(_testListenForwardInfo,1000);
            }).catch(()=>{
                state.timerTestListen = setTimeout(_testListenForwardInfo,1000);
            });
        }
        

        const _getForwardInfo = () => {
            getForwardInfo().then((res) => {
                state.data = res[state.machineId] || [];
            }).catch(() => {
            });
        }
        const handleOnShowList = () => {
            _getForwardInfo();
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }

        const handleRefresh = () => {
            _getForwardInfo();
            ElMessage.success('已刷新')
        }
        const handleAdd = () => {
            saveRow({ ID: 0, Name: '', Port: 0, TargetEP: '127.0.0.1:80', machineId: state.machineId });
        }
        const handleEdit = (row, p) => {
            state.data.forEach(c => {
                c[`NameEditing`] = false;
                c[`PortEditing`] = false;
                c[`TargetEPEditing`] = false;
                c[`BindIPAddressEditing`] = false;
            })
            row[`${p}Editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            saveRow(row);
        }
        const handleDel = (id) => {
            removeForwardInfo(id).then(() => {
                _getForwardInfo();
            })
        }
        const handleStartChange = (row) => {
            saveRow(row);
        }
        const saveRow = (row) => {
            row.Port = parseInt(row.Port);
            addForwardInfo(row).then(() => {
                _getForwardInfo();
            }).catch((err) => {
                ElMessage.error(err);
            });
        }

        onMounted(()=>{
            _getForwardInfo();
            _getForwardIpv4();
            _testTargetForwardInfo();
            _testListenForwardInfo();
        });
        onUnmounted(()=>{
            clearTimeout(state.timerTestTarget);
            clearTimeout(state.timerTestListen);
        });

        return {
            state, handleOnShowList, handleCellClick, handleRefresh, handleAdd, handleEdit, handleEditBlur, handleDel, handleStartChange
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
.green{color:green;font-weight:bold;}
.error{
    color:red;
    font-weight:bold;
    .el-icon{
        vertical-align:text-bottom
    }
}
</style>