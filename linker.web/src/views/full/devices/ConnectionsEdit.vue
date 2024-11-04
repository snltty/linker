<template>
  <el-dialog v-model="state.show" append-to=".app-wrap" :title="`与[${state.machineName}]的链接`" top="1vh" width="780">
        <div>
            <el-table :data="state.data" size="small" border height="500">
                <el-table-column property="RemoteMachineId" label="目标/服务器">
                    <template #default="scope">
                        <div :class="{green:scope.row.Connected}">
                            <p>{{scope.row.IPEndPoint}}</p>
                            <p>ssl : {{scope.row.SSL}}</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="TransactionId" label="事务" width="80">
                    <template #default="scope">
                        <span>{{ state.transactions[scope.row.TransactionId] }}</span>
                    </template>
                </el-table-column>
                <el-table-column property="TransportName" label="协议">
                    <template #default="scope">
                        <div>
                            <p>{{scope.row.TransportName}}({{ state.protocolTypes[scope.row.ProtocolType] }})</p>
                            <p>{{ state.types[scope.row.Type] }} - {{1<<scope.row.BufferSize}}KB</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" label="延迟" width="60">
                    <template #default="scope">
                        <span>{{scope.row.Delay}}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Bytes" label="通信">
                    <template #default="scope">
                        <div>
                            <p>up : {{scope.row.SendBytesText}}</p>
                            <p>down : {{scope.row.ReceiveBytesText}}</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="relay" label="中继节点">
                    <template #default="scope">
                        <div>
                            <el-select disabled :model-value="scope.row.NodeId" placeholder="中继节点" size="large">
                                <el-option v-for="item in state.nodes" :key="item.Id" :label="item.Name" :value="item.Id"/>
                            </el-select>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="54">
                    <template #default="scope">
                        <div>
                            <el-popconfirm v-if="hasTunnelRemove" confirm-button-text="确认" cancel-button-text="取消" title="确定关闭此连接?"
                                @confirm="handleDel(scope.row)">
                                <template #reference>
                                    <el-button type="danger" size="small"><el-icon><Delete /></el-icon></el-button>
                                </template>
                            </el-popconfirm>
                        </div>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch,computed,  onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useConnections, useForwardConnections, useSocks5Connections, useTuntapConnections } from './connections';
import { Delete } from '@element-plus/icons-vue';
import { injectGlobalData } from '@/provide';
import { setRelaySubscribe } from '@/apis/relay';
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const hasTunnelRemove = computed(()=>globalData.value.hasAccess('TunnelRemove')); 

        const connections = useConnections();
        const forwardConnections = useForwardConnections();
        const tuntapConnections = useTuntapConnections();
        const socks5Connections = useSocks5Connections();
        const state = reactive({
            show: true,
            protocolTypes:{1:'tcp',2:'udp',4:'msquic'},
            types:{0:'打洞',1:'中继'},
            transactions:{'forward':'端口转发','tuntap':'虚拟网卡','socks5':'代理转发'},
            machineName:connections.value.currentName,
            data: computed(()=>{
                return [
                    forwardConnections.value.list[connections.value.current],
                    tuntapConnections.value.list[connections.value.current],
                    socks5Connections.value.list[connections.value.current],
                ].filter(c=>!!c);
            }),
            nodes:[],
            timer:0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });
        const handleDel = (row)=>{
            if(!hasTunnelRemove.value) return;
            row.removeFunc(row.RemoteMachineId).then(()=>{
                ElMessage.success('删除成功');
            }).catch(()=>{});
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
            connections.value.updateRealTime(true);
            _setRelaySubscribe();
        });
        onUnmounted(()=>{
            connections.value.updateRealTime(false);
            clearTimeout(state.timer);
        })

        return {
            state,handleDel,hasTunnelRemove
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
</style>