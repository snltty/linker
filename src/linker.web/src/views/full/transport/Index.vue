<template>
    <div class="transport-wrap">
        <el-table stripe  :data="state.list" border size="small" width="100%" :height="`${state.height}px`" >
            <el-table-column prop="Name" :label="$t('status.tunnelName')" width="120"></el-table-column>
            <el-table-column prop="Label" :label="$t('status.tunnelLabel')"></el-table-column>
            <el-table-column prop="ProtocolType" :label="$t('status.tunnelProtocol')" width="60"></el-table-column>
            <el-table-column prop="BufferSize" :label="$t('status.tunnelBuffer')" width="100">
                <template #default="scope">
                    <el-select v-model="scope.row.BufferSize" placeholder="Select" size="small" @change="handleSave">
                        <el-option v-for="(item,index) in state.bufferSize" :key="index" :label="item" :value="index"/>
                    </el-select>
                </template>
            </el-table-column>
            <el-table-column property="Reverse" :label="$t('status.tunnelReverse')" width="64">
                <template #default="scope">
                    <el-switch :disabled="scope.row.DisableReverse" v-model="scope.row.Reverse" @change="handleSave" inline-prompt :active-text="$t('status.tunnelYes')" :inactive-text="$t('status.tunnelNo')" />
                </template>
            </el-table-column>
            <el-table-column property="SSL" :label="$t('status.tunnelSSL')" width="60">
                <template #default="scope">
                    <el-switch :disabled="scope.row.DisableSSL" v-model="scope.row.SSL" @change="handleSave" inline-prompt :active-text="$t('status.tunnelYes')" :inactive-text="$t('status.tunnelNo')" />
                </template>
            </el-table-column>
            <el-table-column property="Disabled" :label="$t('status.tunnelDisanbled')" width="64">
                <template #default="scope">
                    <el-switch v-model="scope.row.Disabled" @change="handleSave" inline-prompt :active-text="$t('status.tunnelYes')" :inactive-text="$t('status.tunnelNo')" style="--el-switch-on-color: red; --el-switch-off-color: #ddd" />
                </template>
            </el-table-column>
            <el-table-column prop="Order" :label="$t('status.tunnelSort')" width="104" fixed="right">
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
import { useI18n } from 'vue-i18n';
export default {
    label:'打洞协议',
    name:'transports',
    order:2,
    components:{Delete,Plus,Top,Bottom},
    setup(props) {
        const {t} = useI18n();
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
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
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