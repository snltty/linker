<template>
    <div>将按顺序通过所有主机获取外网端口，如何使用，交由打洞决定，默认打洞仅使用第一个</div>
    <el-table :data="state.list" border size="small" width="100%" height="300" @cell-dblclick="handleCellClick">
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
        <el-table-column prop="Type" label="类别" >
            <template #default="scope">
                <el-select v-model="scope.row.Type" placeholder="Select" size="small" @change="handleEditBlur(scope.row, 'Type')">
                    <el-option v-for="item in state.types" :key="item.Value" :label="item.Name" :value="item.Value"/>
                </el-select>
            </template>
        </el-table-column>
        <el-table-column prop="Host" label="地址" width="120">
            <template #default="scope">
                <template v-if="scope.row.HostEditing">
                    <el-input autofocus size="small" v-model="scope.row.Host"  @blur="handleEditBlur(scope.row, 'Host')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Host }}
                </template>
            </template>
        </el-table-column>
        <el-table-column property="Disabled" label="禁用" width="60">
            <template #default="scope">
                <el-switch v-model="scope.row.Disabled" @change="handleEditBlur(scope.row, 'Disabled')" inline-prompt active-text="是" inactive-text="否" style="--el-switch-on-color: red; --el-switch-off-color: #ddd" />
            </template>
        </el-table-column>
        <el-table-column prop="Sort" label="调序" width="104" fixed="right">
            <template #default="scope">
                <div>
                    <el-button size="small" @click="handleSort(scope.$index,-1)">
                        <el-icon><Top /></el-icon>
                    </el-button>
                    <el-button size="small" @click="handleSort(scope.$index,1)">
                        <el-icon><Bottom /></el-icon>
                    </el-button>
                </div>
            </template>
        </el-table-column>
        <el-table-column prop="Oper" label="操作" width="104" fixed="right">
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
import { updateTunnelSetServers,getTunnelTypes } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { computed, onMounted, reactive } from 'vue'
export default {
    props:{
        data:{
            type:Array,
            default:[]
        }
    },
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:props.data.sort((a,b)=>a.Disabled - b.Disabled),
            types:[]
        });

        const _getTunnelTypes = ()=>{
            getTunnelTypes().then((res)=>{
                state.types = res;
            });
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`TypeEditing`] = false;
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
            state.list.splice(index+1,0,{Name:'',Host:'',Type:0,Disabled:false});
            handleSave();
        }

        const handleSort = (index,oper)=>{
            const current = state.list[index];
            const outher = state.list[index+oper];

            if(current && outher){
                state.list[index+oper] = current;
                state.list[index] = outher;
            }
            handleSave(state.list);
        }
        
        const handleSave = ()=>{
            state.list = state.list.slice().sort((a,b)=>a.Disabled - b.Disabled);
            updateTunnelSetServers(state.list);
        }

        onMounted(()=>{
            _getTunnelTypes();
        });

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd,handleSort}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>