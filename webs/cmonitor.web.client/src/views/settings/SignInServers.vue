<template>
    <div class="flex">
        <div class="pdr-10 pdb-6 flex-1">
            <el-checkbox v-model="state.sync" label="将更改同步到所有客户端"  />
        </div>
        <div>信标服务器为交换数据服务器（用于登入），使用其中一条作为信标服务器</div>
    </div>
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
        <el-table-column prop="Oper" label="操作" width="150">
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
        </el-table-column>
    </el-table>
</template>
<script>
import { setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Running.Client.Servers || [],
            server:computed(()=>globalData.value.config.Client.Server),
            height: computed(()=>globalData.value.height-130),
            sync:true,
        });

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`HostEditing`] = false;
            })
            row[`${p}Editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            row[`${p}Editing`] = false;
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
            setSignInServers({
                sync:state.sync,
                list:state.list
            });
        }

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd,handleUse}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>