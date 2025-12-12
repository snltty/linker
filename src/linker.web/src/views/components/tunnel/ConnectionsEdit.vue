<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`与[${state.device.MachineName}]的链接`" top="1vh" width="350">
        <div>
            <el-descriptions border size="small" :column="1" label-width="6rem"  overlength-control="wrap">
                <el-descriptions-item label="目标" >{{ state.connection.IPEndPoint }}</el-descriptions-item>
                <el-descriptions-item label="事务" >{{ state.transactions[state.connection.TransactionId] }}</el-descriptions-item>
                <el-descriptions-item label="协议" >
                    <div v-if="state.connection.Connected">
                        <p>{{ state.connection.TransportName }}({{ state.protocolTypes[state.connection.ProtocolType] }}) - {{ state.types[state.connection.Type] }}</p>
                        <p>{{ state.connection.SendBufferRemainingText }} - {{ state.connection.RecvBufferRemainingText }}</p>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item label="SSL" >{{ state.connection.SSL }}</el-descriptions-item>
                
                <el-descriptions-item label="上传" >
                    <div>
                        <p><span>{{ state.connection.SendBytesText }}</span></p>
                    </div>
                </el-descriptions-item>
                 <el-descriptions-item label="下载" >
                    <div>
                        <p><span>{{ state.connection.ReceiveBytesText }}</span></p>
                    </div>
                </el-descriptions-item>
                
                <el-descriptions-item label="中继" >
                    <div>
                        <a v-if="state.connecting" href="javascript:;" class="a-line">
                            <span>操作中.</span><el-icon size="14" class="loading"><Loading /></el-icon>
                        </a>
                        <a v-else href="javascript:;" class="a-line" @click="handleNode">{{ state.nodesDic[state.connection.NodeId] || '选择节点' }}</a>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item label="打洞" >
                    <div>
                        <a v-if="state.connecting" href="javascript:;" class="a-line">
                            <span>操作中.</span><el-icon size="14" class="loading"><Loading /></el-icon>
                        </a>
                        <a v-else href="javascript:;" class="a-line" @click="handlep2p">尝试打洞</a>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item label="延迟" >{{ state.connection.Delay }}</el-descriptions-item>
                 <el-descriptions-item label="操作" >
                    <div>
                        <AccessShow value="TunnelRemove">
                            <el-popconfirm confirm-button-text="确认" cancel-button-text="取消"
                                title="确定关闭此连接?" @confirm="handleDel">
                                <template #reference>
                                    <el-button type="danger" size="small"><el-icon>
                                            <Delete />
                                    </el-icon></el-button>
                                </template>
                            </el-popconfirm>
                        </AccessShow>
                    </div>
                </el-descriptions-item>
            </el-descriptions>
        </div>
    </el-dialog>
    <el-dialog v-model="state.showNodes" :title="$t('server.relayTitle')" width="98%" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="600">
                <el-table-column property="Name" :label="$t('server.relayName')">
                    <template #default="scope">
                        <div>
                            <a :href="scope.row.Url" class="a-line blue" target="_blank">{{ scope.row.Name }}</a>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionsRatio" :label="$t('server.relayConnection')" width="80">
                    <template #default="scope">
                        <span><strong>{{ scope.row.ConnectionsRatio }}</strong></span>
                    </template>
                </el-table-column>
                <el-table-column property="BandwidthEach" :label="$t('server.relaySpeed')" width="140">
                    <template #default="scope">
                        <p>
                            <span>{{ scope.row.BandwidthRatio }}Mbps</span>
                            <span> / </span>
                            <span v-if="scope.row.BandwidthEach == 0">--</span>
                            <span v-else>{{ scope.row.BandwidthEach }}Mbps</span>
                        </p>
                    </template>
                </el-table-column>
                 <el-table-column property="DataEachMonth" :label="$t('server.relayFlow')" width="100">
                    <template #default="scope">
                        <span v-if="scope.row.DataEachMonth == 0">--</span>
                        <span v-else>
                            {{ (scope.row.DataRemain / 1024 / 1024 / 1024).toFixed(2) }}GB
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" :label="$t('server.relayDelay')" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Public" :label="$t('server.relayPublic')" width="50">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Public" size="small" />
                    </template>
                </el-table-column>
                <el-table-column property="Oper" :label="$t('server.relayUse')" width="130">
                    <template #default="scope">
                        <el-button size="small" v-if="(scope.row.Protocol & 1) == 1" @click="handleConnect(scope.row, 1)">TCP</el-button>
                        <el-button size="small" v-if="(scope.row.Protocol & 2) == 2" @click="handleConnect(scope.row, 2)">UDP</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useConnections} from './connections';
import { Delete, Select, ArrowDown,Loading } from '@element-plus/icons-vue';
import { injectGlobalData } from '@/provide';
import { relayConnect, setRelaySubscribe } from '@/apis/relay';
import { useI18n } from 'vue-i18n';
import { removeTunnelConnection, tunnelConnect } from '@/apis/tunnel';
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { Delete, Select, ArrowDown,Loading },
    setup(props, { emit }) {

        const { t } = useI18n();
        const globalData = injectGlobalData();

        const connections = useConnections();
        const connection =  computed(() =>connections.value.device.hook_connection? connections.value.device.hook_connection[connections.value.transactionId] || {} :{});

        const state = reactive({
            show: true,
            protocolTypes: { 1: 'tcp', 2: 'udp', 4: 'msquic' },
            types: { 0: '打洞', 1: '中继', 2: '节点' },
            transactions: { 'forward': '端口转发', 'tuntap': '虚拟网卡', 'socks5': '代理转发' },
            device: connections.value.device,
            transactionId: connections.value.transactionId,
            connecting:computed(()=>connections.value.device.hook_operating?connections.value.device.hook_operating[connections.value.transactionId]:false),
            connection:connection,

            showNodes: false,
            nodes: [],
            nodesDic: {},
            timer: 0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });
        const handleDel = () => {
            removeTunnelConnection(state.device.MachineId,state.transactionId).then(() => {
                ElMessage.success(t('common.oper'));
            }).catch(() => { });
        }

        const _setRelaySubscribe = () => {
            clearTimeout(state.timer);
            setRelaySubscribe().then((res) => {
                state.nodes = res;
                state.nodesDic = res.reduce((a, b) => {
                    a[b.NodeId] = b.Name;
                    return a;
                }, {});
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            }).catch(() => {
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            });
        }

        const handlep2p = ()=>{
            tunnelConnect({
                ToMachineId:state.device.MachineId,
                TransactionId:state.transactionId,
                DenyProtocols:state.transactionId == 'tuntap' ? 4 : 2
            }).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch(()=>{ElMessage.success(t('common.operFail'));})
        }

        const handleNode = () => {
            state.showNodes = true;
        }
        const handleConnect = (row, protocol) => {
            const json = {
                FromMachineId: globalData.value.config.Client.Id,
                TransactionId: state.transactionId,
                ToMachineId: state.device.MachineId,
                NodeId: row.NodeId,
                Protocol: protocol
            };
            relayConnect(json).then(() => {ElMessage.success(t('common.oper')); }).catch(() => {ElMessage.success(t('common.operFail')); });
            state.showNodes = false;
        }

        onMounted(() => {
            connections.value.updateRealTime(true);
            _setRelaySubscribe();
        });
        onUnmounted(() => {
            connections.value.updateRealTime(false);
            clearTimeout(state.timer);
        })

        return {
            state, handleDel,handlep2p, handleNode, handleConnect
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
.blue {
    color: #409EFF;
}
.el-checkbox{font-weight:100}
.dropdown{
    border:1px solid #ddd;
    padding:.4rem;
    font-size:1.3rem;
    border-radius:.4rem;
    position:relative;
    .el-icon{
        vertical-align:middle;
    }

    .badge{
        position:absolute;
        right:-1rem;
        top:-50%;
        border-radius:10px;
        background-color:#f1ae05;
        color:#fff;
        padding:.2rem .6rem;
        font-size:1.2rem;
        
    }
}

@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}
.el-icon{  
    &.loading{
        margin-left:.3rem
        vertical-align:middle;font-weight:bold;
        animation:loading 1s linear infinite;
    }
}

</style>