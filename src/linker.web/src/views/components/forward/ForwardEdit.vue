<template>
  <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" :title="`【${state.machineName}】的端口转发`" top="1vh" width="780">
        <div>
            <div class="t-c head">
                <el-button type="success" size="small" @click="handleAdd" :loading="state.loading">添加</el-button>
                <el-button size="small" @click="handleRefresh">刷新</el-button>
            </div>
            <el-table :data="state.data" size="small" border height="500" @cell-dblclick="handleCellClick">
                <el-table-column property="Name" label="名称" width="100">
                    <template #default="scope">
                        <template v-if="scope.row.NameEditing && scope.row.Started==false">
                            <el-input v-trim autofocus size="small" v-model="scope.row.Name"
                                @blur="handleEditBlur(scope.row, 'Name')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Name')">{{ scope.row.Name || '未知' }}</a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="BufferSize" label="缓冲区" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.BufferSizeEditing && scope.row.Started==false">
                            <el-select v-model="scope.row.BufferSize" placeholder="Select" size="small" :disabled="scope.row.Started" @change="handleEditBlur(scope.row, 'BufferSize')">
                                <el-option v-for="(item,index) in state.bufferSize" :key="index" :label="item" :value="index"/>
                            </el-select>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'BufferSize')">{{ state.bufferSize[scope.row.BufferSize ]}}</a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="BindIPAddress" label="监听IP" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.BindIPAddressEditing && scope.row.Started==false">
                            <el-select v-model="scope.row.BindIPAddress" size="small" :disabled="scope.row.Started" @change="handleEditBlur(scope.row, 'BindIPAddress')">
                                <el-option v-for="item in state.ips" :key="item" :label="item" :value="item"/>
                            </el-select>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'BindIPAddress')">{{ scope.row.BindIPAddress}}</a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="Port" label="监听端口" width="80">
                    <template #default="scope">
                        <template v-if="scope.row.PortEditing && scope.row.Started==false">
                            <el-input v-trim type="number" autofocus size="small" v-model="scope.row.Port"
                                @blur="handleEditBlur(scope.row, 'Port')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'Port')">
                                <template v-if="scope.row.Msg">
                                    <div class="error red" :title="scope.row.Msg">
                                        <span>{{ scope.row.Port }}</span>
                                    </div>
                                </template>
                                <template v-else>
                                    <span :class="{green:scope.row.Started}">{{ scope.row.Port }}</span>
                                </template>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="MachineId" label="目标">
                    <template #default="scope">
                        <template v-if="scope.row.MachineIdEditing && scope.row.Started==false">
                            <el-select v-model="scope.row.MachineId" @change="handleEditBlur(scope.row, 'MachineId')" 
                            filterable remote :loading="state.loading" :remote-method="handleSearch">
                                <template #header>
                                    <div class="t-c">
                                        <div class="page-wrap">
                                            <el-pagination small background layout="prev, pager, next" 
                                            :page-size="state.machineIds.Request.Size" 
                                            :total="state.machineIds.Count" 
                                            :pager-count="5"
                                            :current-page="state.machineIds.Request.Page" @current-change="handlePageChange" />
                                        </div>
                                    </div>
                                </template>
                                <el-option v-for="(item, index) in state.machineIds.List" :key="index" :label="item.MachineName" :value="item.MachineId">
                                </el-option>
                            </el-select>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'MachineId')">
                                <template v-if="state.names[scope.row.MachineId]">
                                    <span>{{ scope.row.MachineName|| '未知'}}</span>
                                </template>
                                <template v-else>
                                    <span class="error red" title="off line">{{ scope.row.MachineName || '未知'}}</span>
                                </template>
                            </a>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="TargetEP" label="目标服务" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.TargetEPEditing && scope.row.Started==false">
                            <el-input v-trim autofocus size="small" v-model="scope.row.TargetEP"
                                @blur="handleEditBlur(scope.row, 'TargetEP')"></el-input>
                        </template>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handleEdit(scope.row, 'TargetEP')">
                                <template v-if="scope.row.TargetMsg">
                                    <div class="error red" :title="scope.row.TargetMsg">
                                        <span>{{ scope.row.TargetEP }}</span>
                                    </div>
                                </template>
                                <template v-else>
                                    <span :class="{green:scope.row.Started}">{{ scope.row.TargetEP }}</span>
                                </template>
                            </a>
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
import {  onMounted,onUnmounted,reactive, watch } from 'vue';
import { getForwardInfo, removeForwardInfo, addForwardInfo ,getForwardIpv4,testTargetForwardInfo } from '@/apis/forward'
import { ElMessage } from 'element-plus';
import {Delete} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { useForward } from './forward';
import { getSignInIds,getSignInNames } from '@/apis/signin';
export default {
    props: ['data','modelValue'],
    emits: ['update:modelValue'],
    components:{Delete},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const forward = useForward();
        const state = reactive({
            show: true,
            machineId: forward.value.machineId,
            machineName: forward.value.machineName,
            data: [],
            ips:[],
            bufferSize:globalData.value.bufferSize,
            loading:false,
            machineIds:{
                Request: {
                    Page: 1, Size:10, Name: ''
                },
                Count: 0,
                List: []
            },
            timer:0,
            timer1:0,
            editing:false,
            names:{}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _testTargetForwardInfo = ()=>{
            clearTimeout(state.timer);
            testTargetForwardInfo(forward.value.machineId).then((res)=>{
               state.timer = setTimeout(_testTargetForwardInfo,1000);
            }).catch(()=>{
                state.timer = setTimeout(_testTargetForwardInfo,1000);
            });
        }

        const _getForwardIpv4 = ()=>{
            getForwardIpv4().then((res)=>{
                res.splice(0,0,'127.0.0.1');
                res.splice(0,0,'0.0.0.0');
                state.ips = res;
            }).catch(()=>{});
        }
        const _getForwardInfo = () => {
            clearTimeout(state.timer1);
            if(state.editing==false){
                getForwardInfo(state.machineId).then((res) => {
                    state.data = res;
                    state.timer1 = setTimeout(_getForwardInfo,1000);
                }).catch(() => {
                    state.timer1 = setTimeout(_getForwardInfo,1000);
                });
            }else{
                state.timer1 = setTimeout(_getForwardInfo,1000);
            }
        }
        const handleRefresh = () => {
            _getForwardInfo();
            ElMessage.success('已刷新')
        }

        const _getSignInNames = ()=>{
            getSignInNames().then((res)=>{
                state.names = res.filter(c=>c.Online).reduce((json,value)=>{ json[value.MachineId]=true; return json; },{});
            }).catch(()=>{});
        }

        const handleSearch = (name)=>{
            state.machineIds.Request.Name = name;
            _getMachineIds();
        }
        const _getMachineIds = ()=>{
            state.loading = true;
            getSignInIds(state.machineIds.Request).then((res)=>{
                state.loading = false;
                state.machineIds.Request = res.Request;
                state.machineIds.Count = res.Count;
                state.machineIds.List = res.List;
            }).catch((e)=>{
                state.loading = false;
            });
        }
        const handlePageChange = (page)=>{
            state.machineIds.Request.Page = page;
            _getMachineIds();
        }

        const handleOnShowList = () => {
            _getMachineIds();
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleAdd = () => {
            saveRow({ ID: 0, Name: '', Port: 0, TargetEP: '127.0.0.1:80', machineId: '' });
        }
        const handleEdit = (row, p) => {
            if(row.Started){
                ElMessage.error('请先停止');
                return;
            }
            state.data.forEach(c => {
                c[`NameEditing`] = false;
                c[`PortEditing`] = false;
                c[`TargetEPEditing`] = false;
                c[`BindIPAddressEditing`] = false;
                c[`BufferSizeEditing`] = false;
                c[`MachineIdEditing`] = false;
                
            })
            row[`${p}Editing`] = true;
            state.editing = true;
        }
        const handleEditBlur = (row, p) => {
            if(row.Started){
                ElMessage.error('请先停止');
                return;
            }
            row[`${p}Editing`] = false;
            state.editing = false;

            const machine = state.machineIds.List.find(c=>c.MachineId == row.MachineId);
            if(machine){
                row.MachineName = machine.MachineName;
            }
            try{row[p] = row[p].trim();}catch(w){}
            saveRow(row);
        }
        const handleDel = (id) => {
            removeForwardInfo({machineId:state.machineId, Id: id }).then(() => {
                _getForwardInfo();
            })
        }
        const handleStartChange = (row) => {
            
            saveRow(row);
        }
        const saveRow = (row) => {
            state.loading = true;
            row.Port = parseInt(row.Port);
            addForwardInfo({machineId:state.machineId,data:row}).then(() => {
                state.loading = false;
                _getForwardInfo();
            }).catch((err) => {
                state.loading = false;
                ElMessage.error(err);
            });
        }

        onMounted(()=>{
            _getForwardInfo();
            _getForwardIpv4();
            _testTargetForwardInfo();
            _getSignInNames();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
            clearTimeout(state.timer1);
        });

        return {
            state, handleOnShowList, handleCellClick,handleRefresh, handleAdd, handleEdit, handleEditBlur, handleDel, handleStartChange,
            handleSearch,handlePageChange
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
.green{color:green;font-weight:bold;}
.error{
    font-weight:bold;
    .el-icon{
        vertical-align:text-bottom
    }
}
</style>