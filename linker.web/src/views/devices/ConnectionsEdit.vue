<template>
  <el-dialog v-model="state.show" append-to=".app-wrap" title="隧道链接" top="1vh" width="700">
        <div>
            <el-table :data="state.data" size="small" border height="500">
                <el-table-column property="RemoteMachineId" label="目标">
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
                <el-table-column property="Delay" label="延迟" width="80">
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
                <el-table-column label="操作" width="54">
                    <template #default="scope">
                        <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="确定关闭此连接?"
                            @confirm="handleDel(scope.row)">
                            <template #reference>
                                <el-button type="danger" size="small"><el-icon><Delete /></el-icon></el-button>
                            </template>
                        </el-popconfirm>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch,computed } from 'vue';
import { ElMessage } from 'element-plus';
import { useConnections, useForwardConnections, useTuntapConnections } from './connections';
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

        const connections = useConnections();
        const forwardConnections =useForwardConnections();
        const tuntapConnections = useTuntapConnections();
        const state = reactive({
            show: true,
            protocolTypes:{1:'tcp',2:'udp',4:'msquic'},
            types:{0:'打洞',1:'中继'},
            transactions:{'forward':'端口转发','tuntap':'虚拟网卡'},
            data: computed(()=>{
                return [
                    forwardConnections.value.list[connections.value.current],
                    tuntapConnections.value.list[connections.value.current],
                ].filter(c=>!!c);
            }),
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
            row.removeFunc(row.RemoteMachineId).then(()=>{
                ElMessage.success('删除成功');
            }).catch(()=>{});
        }

        return {
            state,handleDel
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
.green{color:green}
</style>