<template>
    <div class="flex">
        <div class="pdr-10 pdb-6 flex-1">
            <el-checkbox v-model="state.sync" label="将更改同步到所有客户端"  />
        </div>
        <div>将按顺序使用打洞协议进行打洞尝试</div>
    </div>
    <el-table :data="state.list" border size="small" width="100%" :height="`${state.height}px`" >
        <el-table-column prop="Name" label="名称" width="120"></el-table-column>
        <el-table-column prop="Label" label="说明"></el-table-column>
        <el-table-column prop="ProtocolType" label="协议" width="60"></el-table-column>
        <el-table-column prop="BufferSize" label="缓冲区" width="100">
            <template #default="scope">
                <el-select v-model="scope.row.BufferSize" placeholder="Select" size="small" @change="handleSave">
                    <el-option v-for="(item,index) in state.bufferSize" :key="index" :label="item" :value="index"/>
                </el-select>
            </template>
        </el-table-column>
        <el-table-column property="Reverse" label="反向" width="60">
            <template #default="scope">
                <el-switch :disabled="scope.row.DisableReverse" v-model="scope.row.Reverse" @change="handleSave" inline-prompt active-text="是" inactive-text="否" />
            </template>
        </el-table-column>
        <el-table-column property="SSL" label="SSL" width="60">
            <template #default="scope">
                <el-switch :disabled="scope.row.DisableSSL" v-model="scope.row.SSL" @change="handleSave" inline-prompt active-text="是" inactive-text="否" />
            </template>
        </el-table-column>
        <el-table-column property="Disabled" label="禁用" width="60">
            <template #default="scope">
                <el-switch v-model="scope.row.Disabled" @change="handleSave" inline-prompt active-text="是" inactive-text="否" style="--el-switch-on-color: red; --el-switch-off-color: #ddd" />
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
    </el-table>
</template>
<script>
import { getTunnelTransports,setTunnelTransports } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:[],
            height: computed(()=>globalData.value.height-130),
            bufferSize:globalData.value.bufferSize,
            sync:true,
        });

        const _getTunnelTransports = ()=>{
            getTunnelTransports().then((res)=>{
                state.list = res;
            });
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
            setTunnelTransports({
                sync:state.sync,
                List:state.list
            }).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.success('操作失败');
            });
        }


        onMounted(()=>{
            _getTunnelTransports();
        });

        return {state,handleSort,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>