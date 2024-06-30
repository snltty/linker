<template>
    <div class="flex">
        <div class="pdr-10 pdb-6 flex-1">
            <el-checkbox v-model="state.sync" label="将更改同步到所有客户端"  />
        </div>
        <div>打洞时，排除这些IP(比如VPN，虚拟网卡IP)</div>
    </div>
    <el-table :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
        <el-table-column prop="IPAddress" label="IP">
            <template #default="scope">
                <template v-if="scope.row.IPAddressEditing">
                    <el-input autofocus size="small" v-model="scope.row.IPAddress"  @blur="handleEditBlur(scope.row, 'IPAddress')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.IPAddress }}
                </template>
            </template>
        </el-table-column>
        <el-table-column prop="Mask" label="掩码">
            <template #default="scope">
                <template v-if="scope.row.MaskEditing">
                    <el-input autofocus size="small" v-model="scope.row.Mask"  @blur="handleEditBlur(scope.row, 'Mask')"></el-input>
                </template>
                <template v-else>
                    {{ scope.row.Mask }}
                </template>
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
import { setTunnelExcludeIPs } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:((globalData.value.config.Running.Tunnel || {ExcludeIPs:[]}).ExcludeIPs || [{IPAddress:'0.0.0.0',Mask:32}]),
            types:[],
            height: computed(()=>globalData.value.height-130),
            sync:true
        });

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`IPAddressEditing`] = false;
                c[`MaskEditing`] = false;
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
            state.list.splice(index+1,0,{IPAddress:'0.0.0.0',Mask:32});
            handleSave();
        }

        const handleSave = ()=>{
            state.list.forEach(c=>{
                c.Mask = parseInt(c.Mask);
            })
            setTunnelExcludeIPs({
                sync:state.sync,
                list:state.list
            }).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.success('操作失败');
            });;
        }

        onMounted(() => {
            if(state.list.length == 0){
               state.list =  [{IPAddress:'0.0.0.0',Mask:32}];
            }
        });


        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>