<template>
    <el-table stripe  :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
        <el-table-column prop="Name" label="名称" width="100">
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
        <el-table-column prop="Id" label="Id" >
            <template #default="scope">
                <template v-if="scope.row.IdEditing">
                    <el-input autofocus size="small" v-model="scope.row.Id"
                        @blur="handleEditBlur(scope.row, 'Id')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Id }}
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Password" label="密码" >
            <template #default="scope">
                <template v-if="scope.row.PasswordEditing">
                    <el-input type="password" show-password size="small" v-model="scope.row.Password" @blur="handleEditBlur(scope.row, 'Password')"></el-input>
                </template>
                <template v-else>{{ scope.row.Password.replace(/.{1}/g,'*') }}</template>
            </template>
        </el-table-column>
        <el-table-column prop="Oper" label="操作" width="110">
            <template #default="scope">
                <div>
                    <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDel(scope.$index)">
                        <template #reference>
                            <el-button type="danger" size="small">
                                <el-icon><Delete /></el-icon>
                            </el-button>
                        </template>
                    </el-popconfirm>
                    <el-button type="primary" size="small" @click="handleAdd(scope.$index)">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                </div>
            </template>
        </el-table-column>
    </el-table>
</template>
<script>
import { setSignInGroups } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive, watch } from 'vue'
import { Delete,Plus,Select } from '@element-plus/icons-vue';
export default {
    components:{Delete,Plus,Select },
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Groups || [],
            height: computed(()=>globalData.value.height-90),
        });
        watch(()=>globalData.value.config.Client.Groups,()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                state.list = globalData.value.config.Client.Groups;
            }
        })

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`IdEditing`] = false;
                c[`PasswordEditing`] = false;
            })
            row[`${p}Editing`] = true;
            row[`__editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
            row[`__editing`] = false;
            handleSave();
        }

        const handleDel = (index)=>{
            state.list.splice(index,1);
            handleSave();
        }
        const handleAdd = (index)=>{
            if(state.list.filter(c=>c.Id == '' || c.Name == '').length > 0){
                return;
            }
            state.list.splice(index+1,0,{Name:'',Id:'',Password:''});
            handleSave();
        }

        const handleSave = ()=>{
            setSignInGroups(state.list).then(()=>{
                ElMessage.success('已操作，请在右下角【信标服务器】重连');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>