<template>
    <el-form-item label="中继密钥">
        <el-input type="password" show-password v-model="state.list.SecretKey" maxlength="36" @change="handleSave" />
        <el-checkbox v-model="state.list.SSL" label="使用ssl" size="large" @change="handleSave" />
        <el-checkbox v-model="state.list.Disabled" label="禁用中继" size="large" @change="handleSave" />
        <a href="javascript:;" @click="state.show=true" class="delay a-line" :class="{red:state.nodes.length==0,green:state.nodes.length>0}">
            中继节点 : {{state.nodes.length}}
        </a>
    </el-form-item>
    
    <el-dialog v-model="state.show" title="中继节点" width="760" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="500">
                <el-table-column property="Name" label="名称"></el-table-column>
                <el-table-column property="MaxGbTotal" label="月流量" width="160">
                    <template #default="scope">
                        <span v-if="scope.row.MaxGbTotal == 0">无限制</span>
                        <span v-else>{{ (scope.row.MaxGbTotalLastBytes/1024/1024/1024).toFixed(2) }}GB / {{ scope.row.MaxGbTotal }}GB</span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidth" label="连接带宽" width="80">
                    <template #default="scope">
                        <span v-if="scope.row.MaxBandwidth == 0">无限制</span>
                        <span v-else>{{ scope.row.MaxBandwidth }}Mbps</span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidthTotal" label="总带宽" width="80">
                    <template #default="scope">
                        <span v-if="scope.row.MaxBandwidthTotal == 0">无限制</span>
                        <span v-else>{{ scope.row.MaxBandwidthTotal }}Mbps</span>
                    </template>
                </el-table-column>
                <el-table-column property="BandwidthRatio" label="带宽速率" width="66">
                    <template #default="scope">
                        <span>{{ (scope.row.BandwidthRatio*100).toFixed(2) }}%</span>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionRatio" label="连接数" width="60">
                    <template #default="scope">
                        <span>{{ (scope.row.ConnectionRatio*100).toFixed(2) }}%</span>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" label="延迟" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Public" label="公开" width="60">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Public " size="small" />
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { setRelayServers, setRelaySubscribe } from '@/apis/relay';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, onUnmounted, reactive, watch } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Relay.Server,
            show:false,
            nodes:[],
            timer:0
        });
        watch(()=>globalData.value.config.Client.Relay.Server,()=>{
            state.list.Delay = globalData.value.config.Client.Relay.Server.Delay;
        })

        const handleSave = ()=>{
            setRelayServers(state.list).then(()=>{
                ElMessage.success('已操作');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });;
        }

        const _setRelaySubscribe = ()=>{
            setRelaySubscribe().then((res)=>{
                state.nodes = res;
                state.timer = setTimeout(_setRelaySubscribe,1000);
            }).catch(()=>{
                state.timer = setTimeout(_setRelaySubscribe,1000);
            });
        }
        onMounted(()=>{
            _setRelaySubscribe();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        })

        return {state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.delay{margin-left:3rem;}
</style>