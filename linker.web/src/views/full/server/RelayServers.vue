<template>
    <el-table :data="state.list" border size="small" width="100%" :height="`${state.height}px`" @cell-dblclick="handleCellClick">
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
        <el-table-column prop="RelayType" label="类别" width="100">
            <template #default="scope">
                <el-select disabled v-model="scope.row.RelayType" placeholder="Select" size="small" @change="handleEditBlur(scope.row, 'RelayType')">
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
                    <span :class="{red:!scope.row.Available,green:scope.row.Available}">{{ scope.row.Host }}</span>
                    <span :class="{red:scope.row.Delay==-1,green:scope.row.Delay<500 &&scope.row.Delay>=0 ,yellow:scope.row.Delay>=500}"> - {{scope.row.Delay}}ms</span>
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
        <el-table-column property="SSL" label="SSL" width="60">
            <template #default="scope">
                <el-switch v-model="scope.row.SSL" @change="handleEditBlur(scope.row, 'SSL')" inline-prompt active-text="是" inactive-text="否"/>
            </template>
        </el-table-column>
        <el-table-column property="Disabled" label="禁用" width="60">
            <template #default="scope">
                <el-switch v-model="scope.row.Disabled" @change="handleEditBlur(scope.row, 'Disabled')" inline-prompt active-text="是" inactive-text="否" style="--el-switch-on-color: red; --el-switch-off-color: #ddd" />
            </template>
        </el-table-column>
        <!-- <el-table-column prop="Sort" label="调序" width="104" fixed="right">
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
        </el-table-column> -->
    </el-table>
</template>
<script>
import { setRelayServers,getRelayTypes } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, inject, onMounted, reactive, watch } from 'vue'
import { Delete,Plus,Top,Bottom } from '@element-plus/icons-vue';
export default {
    label:'中继服务器',
    name:'relayServers',
    order:4,
    components:{Delete,Plus,Top,Bottom},
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Relay.Servers.sort((a,b)=>a.Disabled - b.Disabled),
            types:[],
            height: computed(()=>globalData.value.height-90)
        });
        watch(()=>globalData.value.config.Client.Relay.Servers,()=>{
            if(state.list.filter(c=>c['__editing']).length == 0){
                state.list = globalData.value.config.Client.Relay.Servers.sort((a,b)=>a.Disabled - b.Disabled);
            }
        })

        const _getRelayTypes = ()=>{
            getRelayTypes().then((res)=>{
                state.types = res;
            });
        }

        const handleCellClick = (row, column) => {
            handleEdit(row, column.property);
        }
        const handleEdit = (row, p) => {
            state.list.forEach(c => {
                c[`NameEditing`] = false;
                c[`RelayTypeEditing`] = false;
                c[`HostEditing`] = false;
                c[`SecretKeyEditing`] = false;
            })
            row[`__editing`] = true;
            row[`${p}Editing`] = true;
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
            state.list.splice(index+1,0,{Name:'',Host:'',RelayType:0,SecretKey:'snltty',Disabled:false});
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
            setRelayServers(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });;
        }

        onMounted(()=>{
            _getRelayTypes();
        });

        return {state,handleCellClick,handleEditBlur,handleDel,handleAdd,handleSort}
    }
}
</script>
<style lang="stylus" scoped>
.green,.red{font-weight:bold;}
</style>