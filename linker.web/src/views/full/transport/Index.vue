<template>
    <div class="transport-wrap">
        <el-table stripe  :data="state.list" border size="small" width="100%" :height="`${state.height}px`" >
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
            <el-table-column prop="Order" label="调序" width="104" fixed="right">
                <template #default="scope">
                    <div>
                        <el-input-number v-model="scope.row.Order" :min="1" :max="255" @change="handleOrderChange" size="small" />
                    </div>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script>
import { setTunnelTransports } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed,reactive, watch } from 'vue'
import { Delete,Plus,Top,Bottom } from '@element-plus/icons-vue';
export default {
    label:'打洞协议',
    name:'transports',
    order:2,
    components:{Delete,Plus,Top,Bottom},
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Tunnel.Transports.sort((a,b)=>a.Order - b.Order),
            height: computed(()=>globalData.value.height-20),
            bufferSize:globalData.value.bufferSize
        });
        watch(()=>globalData.value.config.Client.Tunnel.Transports,()=>{
            state.list = globalData.value.config.Client.Tunnel.Transports.sort((a,b)=>a.Order - b.Order);
        });

        const handleOrderChange = ()=>{
            handleSave(state.list);
        }    
        const handleSave = ()=>{
            state.list = state.list.slice().sort((a,b)=>a.Order - b.Order);
            setTunnelTransports(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }

        return {state,handleOrderChange,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.transport-wrap{
    padding:1rem
    font-size:1.3rem;
    color:#555;
    a{color:#333;}
}
</style>