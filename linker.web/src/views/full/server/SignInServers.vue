<template>
    <el-table :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
        <el-table-column prop="Name" label="名称">
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
        <el-table-column prop="Host" label="地址" >
            <template #default="scope">
                <template v-if="scope.row.HostEditing">
                    <el-input autofocus size="small" v-model="scope.row.Host"
                        @blur="handleEditBlur(scope.row, 'Host')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Host }}
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="SecretKey" label="秘钥" >
            <template #default="scope">
                <template v-if="scope.row.SecretKeyEditing">
                    <el-input type="password" show-password size="small" v-model="scope.row.SecretKey" @blur="handleEditBlur(scope.row, 'SecretKey')"></el-input>
                </template>
                <template v-else></template>
            </template>
        </el-table-column>
        <!-- <el-table-column prop="Oper" label="操作" width="150">
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
                    <template v-if="state.server != scope.row.Host">
                        <el-button size="small" @click="handleUse(scope.$index)">
                            <el-icon><Select /></el-icon>
                        </el-button>
                    </template>
                </div>
            </template>
        </el-table-column> -->
    </el-table>
</template>
<script>
import { setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, inject, reactive, watch } from 'vue'
import { Delete,Plus,Select } from '@element-plus/icons-vue';
export default {
    label:'信标服务器',
    name:'signInServers',
    order:0,
    components:{Delete,Plus,Select },
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Servers || [],
            server:computed(()=>globalData.value.config.Client.ServerInfo.Host),
            height: computed(()=>globalData.value.height-90),
        });
        watch(()=>globalData.value.config.Client.Servers,()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                state.list = globalData.value.config.Client.Servers;
            }
        })

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`HostEditing`] = false;
                c[`SecretKeyEditing`] = false;
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
            if(state.list.filter(c=>c.Host == '' || c.Name == '').length > 0){
                return;
            }
            state.list.splice(index+1,0,{Name:'',Host:''});
            handleSave();
        }
        const handleUse = (index)=>{
            const temp = state.list[index];
            state.list[index] = state.list[0];
            state.list[0] = temp;
            handleSave();
        }

        const handleSave = ()=>{
            setSignInServers(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.success('操作失败');
            });;
        }

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd,handleUse}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>