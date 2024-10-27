<template>
  <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" :title="`【${machineName}】的内网穿透`" top="1vh" width="700">
        <div>
            <div class="t-c head">
                <el-button type="success" size="small" @click="handleAdd">添加</el-button>
                <el-button size="small" @click="handleRefresh">刷新</el-button>
            </div>
            <el-table :data="state.data" size="small" border height="500" @cell-dblclick="handleCellClick">
                <el-table-column property="Name" label="名称">
                    <template #default="scope">
                        <template v-if="scope.row.NameEditing && scope.row.Started==false ">
                            <el-input autofocus size="small" v-model="scope.row.Name"
                                @blur="handleEditBlur(scope.row, 'Name')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.Name }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column prop="BufferSize" label="缓冲区" width="100">
                    <template #default="scope">
                        <span>{{ 1<<scope.row.BufferSize }}KB</span>
                    </template>
                </el-table-column>
                <el-table-column property="Temp" label="远程端口/域名" width="160">
                    <template #default="scope">
                        <template v-if="scope.row.TempEditing && scope.row.Started==false">
                            <el-input autofocus size="small" v-model="scope.row.Temp"
                                @blur="handleEditBlur(scope.row, 'Temp')"></el-input>
                        </template>
                        <template v-else>
                            <template v-if="scope.row.Msg">
                                <div class="error red" :title="scope.row.Msg">
                                    <span>{{ scope.row.Temp }}</span>
                                    <el-icon size="20"><WarnTriangleFilled /></el-icon>
                                </div>
                            </template>
                            <template v-else><span :class="{green:scope.row.Started}">{{ scope.row.Temp }}</span></template>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="LocalEP" label="本机服务" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.LocalEPEditing && scope.row.Started==false">
                            <el-input autofocus size="small" v-model="scope.row.LocalEP"
                                @blur="handleEditBlur(scope.row, 'LocalEP')"></el-input>
                        </template>
                        <template v-else>
                            <template v-if="scope.row.LocalMsg">
                                <div class="error red" :title="scope.row.LocalMsg">
                                    <span>{{ scope.row.LocalEP }}</span>
                                    <el-icon size="20"><WarnTriangleFilled /></el-icon>
                                </div>
                            </template>
                            <template v-else>
                                <span :class="{green:scope.row.Started}">{{ scope.row.LocalEP }}</span>
                            </template>
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="Started" label="状态" width="60">
                    <template #default="scope">
                        <el-switch v-model="scope.row.Started" @change="handleStartChange(scope.row)" inline-prompt
                            active-text="是" inactive-text="否" />
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
import { onMounted, onUnmounted, reactive, watch } from 'vue';
import { getSForwardInfo, removeSForwardInfo, addSForwardInfo,testLocalSForwardInfo } from '@/apis/sforward'
import { ElMessage } from 'element-plus';
import {WarnTriangleFilled,Delete} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { useSforward } from './sforward';
export default {
    props: ['data','modelValue'],
    emits: ['update:modelValue'],
    components:{WarnTriangleFilled,Delete},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const sforward = useSforward();
        const state = reactive({
            bufferSize:globalData.value.bufferSize,
            show: true,
            data: [],
            timerTestLocal:0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const _testLocalSForwardInfo = ()=>{
            console.log(sforward.value.machineid);
            testLocalSForwardInfo(sforward.value.machineid).then((res)=>{
               state.timerTestLocal = setTimeout(_testLocalSForwardInfo,1000);
            }).catch(()=>{
                state.timerTestLocal = setTimeout(_testLocalSForwardInfo,1000);
            });
        }
        const _getSForwardInfo = () => {
            getSForwardInfo(sforward.value.machineid).then((res) => {
                res.forEach(c=>{
                    c.Temp = (c.Domain || c.RemotePort).toString();
                    c.RemotePort = 0;
                    c.Domain = '';
                });
                state.data = res;
            }).catch(() => {
            });
        }
        const handleOnShowList = () => {
            _getSForwardInfo();
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }

        const handleRefresh = () => {
            _getSForwardInfo();
            ElMessage.success('已刷新')
        }
        const handleAdd = () => {
            const row = { Id: 0, Name: '', RemotePort: 0, LocalEP: '127.0.0.1:80',Domain:'',Temp:'' };
            addSForwardInfo({machineid:sforward.value.machineid,data:row}).then(() => {
                setTimeout(()=>{
                    _getSForwardInfo();
                },100)
            }).catch((err) => {
                ElMessage.error(err);
            });
        }
        const handleEdit = (row, p) => {
            if(row.Started){
                ElMessage.error('请先停止运行');
                return;
            }
            state.data.forEach(c => {
                c[`NameEditing`] = false;
                c[`RemotePortEditing`] = false;
                c[`LocalEPEditing`] = false;
                c[`DomainEditing`] = false;
                c[`TempEditing`] = false;
            })
            row[`${p}Editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            if(row.Started){
                ElMessage.error('请先停止运行');
                return;
            }
            row[`${p}Editing`] = false;
            saveRow(row);
        }
        const handleDel = (id) => {
            removeSForwardInfo(id).then(() => {
                _getSForwardInfo();
            })
        }
        const handleStartChange = (row) => {
            saveRow(row);
        }
        const saveRow = (row) => {
            if(!row.Temp) return;
            if(/^\d+$/.test(row.Temp)){
                row.RemotePort = parseInt(row.Temp);
            }else{
                row.Domain = row.Temp;
            }

            addSForwardInfo({machineid:sforward.value.machineid,data:row}).then(() => {
                setTimeout(()=>{
                    _getSForwardInfo();
                },100)
            }).catch((err) => {
                ElMessage.error(err);
            });
        }
        onMounted(()=>{
            _getSForwardInfo();
            _testLocalSForwardInfo();
        });
        onUnmounted(()=>{
            clearTimeout(state.timerTestLocal);
        })

        return {
            state,machineName:sforward.value.machineName, 
            handleOnShowList, handleCellClick, handleRefresh, handleAdd, handleEdit, handleEditBlur, handleDel, handleStartChange
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}

.error{
    font-weight:bold;
    .el-icon{
        vertical-align:text-bottom
    }
}
</style>