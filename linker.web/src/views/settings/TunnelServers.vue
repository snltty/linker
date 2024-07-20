<template>
    <Version ckey="tunnelWanPortProtocols"/>
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
        <el-table-column prop="ProtocolType" label="协议" width="100">
            <template #default="scope">
                <div>
                    <el-select v-model="scope.row.ProtocolType" placeholder="Select" size="small" @change="handleEditBlur(scope.row, 'ProtocolType')">
                        <el-option v-for="(item,index) in scope.row.Protocols" :key="+index" :label="item" :value="+index"/>
                    </el-select>
                </div>
                
            </template>
        </el-table-column>
        <el-table-column prop="Type" label="类别" width="100">
            <template #default="scope">
                <el-select v-model="scope.row.Type" placeholder="Select" size="small" @change="handleEditBlur(scope.row, 'Type')">
                    <el-option v-for="item in state.types" :key="item.Value" :label="item.Name" :value="item.Value"/>
                </el-select>
            </template>
        </el-table-column>
        <el-table-column prop="Host" label="地址">
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
import { setTunnelServers,getTunnelTypes } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
import Version from './Version.vue';
import { Delete,Plus,Top,Bottom } from '@element-plus/icons-vue';
export default {
    label:'端口服务器',
    name:'tunnelServers',
    order:1,
    components:{Version,Delete,Plus,Top,Bottom},
    setup(props) {
        const globalData = injectGlobalData();
        const list = ((globalData.value.config.Running.Tunnel || {Servers:[]}).Servers || []).sort((a,b)=>a.Disabled - b.Disabled);
        const state = reactive({
            list:list,
            types:[],
            height: computed(()=>globalData.value.height-127)
        });

        const _getTunnelTypes = ()=>{
            getTunnelTypes().then((res)=>{
                state.types = res;
                initProtocols(state.list);
            });
        }
        const initProtocols = (list)=>{
            list.forEach(c=>{
                c.Protocols = state.types.filter(d=>d.Value == c.Type)[0].Protocols;
                if(!c.Protocols[c.ProtocolType]){
                    c.ProtocolType = +Object.keys(c.Protocols)[0];
                }
            });
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            initProtocols([row])
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`TypeEditing`] = false;
                c[`HostEditing`] = false;
                c[`ProtocolTypeEditing`] = false;
            })
            row[`${p}Editing`] = true;
        }
        const handleEditBlur = (row, p) => {
            initProtocols([row])
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
            const row = {Name:'',Host:'',Type:0,Disabled:false,ProtocolType:2};
            initProtocols([row]);
            state.list.splice(index+1,0,row);
            
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
            setTunnelServers(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.success('操作失败');
            });;
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