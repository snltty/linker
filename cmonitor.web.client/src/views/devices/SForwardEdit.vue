<template>
  <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" title="服务器端口转发到本机" top="1vh" width="700">
        <div>
            <div class="t-c head">
                <el-button type="success" size="small" @click="handleAdd">添加</el-button>
                <el-button size="small" @click="handleRefresh">刷新</el-button>
            </div>
            <el-table :data="state.data" size="small" border height="500" @cell-dblclick="handleCellClick">
                <el-table-column property="ID" label="ID" width="60" />
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
                <el-table-column property="Temp" label="远程端口/域名" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.TempEditing">
                            <el-input autofocus size="small" v-model="scope.row.Temp"
                                @blur="handleEditBlur(scope.row, 'Temp')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.Temp }}
                        </template>
                    </template>
                </el-table-column>
                <el-table-column property="LocalEP" label="本机服务" width="140">
                    <template #default="scope">
                        <template v-if="scope.row.LocalEPEditing">
                            <el-input autofocus size="small" v-model="scope.row.LocalEP"
                                @blur="handleEditBlur(scope.row, 'LocalEP')"></el-input>
                        </template>
                        <template v-else>
                            {{ scope.row.LocalEP }}
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
import { inject, onMounted, reactive, watch } from 'vue';
import { getSForwardInfo, removeSForwardInfo, addSForwardInfo } from '@/apis/sforward'
import { ElMessage } from 'element-plus';
export default {
    props: ['data','modelValue'],
    emits: ['update:modelValue'],
    setup(props, { emit }) {

        const sforward = inject('sforward');
        const state = reactive({
            show: true,
            data: [],
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _getSForwardInfo = () => {
            getSForwardInfo().then((res) => {
                let arr = (res|| []);
                arr.forEach(c=>{
                    c.Temp = (c.Domain || c.RemotePort).toString();
                    c.RemotePort = 0;
                    c.Domain = '';
                })
                state.data = arr;
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
            saveRow({ Id: 0, Name: '', RemotePort: 0, LocalEP: '127.0.0.1:80',Domain:'',Temp:'' });
        }
        const handleEdit = (row, p) => {
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
                if(row.RemotePort <= 1024 || row.RemotePort > 65535){
                    ElMessage.error('端口范围1025-65535');
                    return;
                }
            }else{
                row.Domain = row.Temp;
            }

            addSForwardInfo(row).then(() => {
                setTimeout(()=>{
                    _getSForwardInfo();
                },1000)
            }).catch((err) => {
                ElMessage.error(err);
            });
        }

        onMounted(()=>{
            _getSForwardInfo();
        })

        return {
            state, handleOnShowList, handleCellClick, handleRefresh, handleAdd, handleEdit, handleEditBlur, handleDel, handleStartChange
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
</style>