<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`与[${state.machineName}]的链接`" top="1vh" width="780">
        <div>
            <el-table :data="state.data" size="small" border height="500">
                <el-table-column property="RemoteMachineId" label="目标/服务器">
                    <template #default="scope">
                        <div :class="{ green: scope.row.Connected }">
                            <p>{{ scope.row.IPEndPoint }}</p>
                            <p>ssl : {{ scope.row.SSL }}</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="TransactionId" label="事务" width="80">
                    <template #default="scope">
                        <span>{{ state.transactions[scope.row.TransactionId] }}</span>
                    </template>
                </el-table-column>
                <el-table-column property="TransportName" label="协议"  width="120">
                    <template #default="scope">
                        <div>
                            <p>{{ scope.row.TransportName }}({{ state.protocolTypes[scope.row.ProtocolType] }})</p>
                            <p>{{ state.types[scope.row.Type] }} - {{ 1 << scope.row.BufferSize }}KB</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" label="延迟" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Bytes" label="通信">
                    <template #default="scope">
                        <div>
                            <p>up : {{ scope.row.SendBytesText }}</p>
                            <p>down : {{ scope.row.ReceiveBytesText }}</p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="relay" label="中继节点">
                    <template #default="scope">
                        <div>
                            <p>
                                <span>中继 : </span>
                                <a v-if="state.relayOperatings[scope.row.RemoteMachineId]" href="javascript:;" class="a-line">
                                    <span>操作中.</span><el-icon size="14" class="loading"><Loading /></el-icon>
                                </a>
                                <a v-else href="javascript:;" class="a-line" @click="handleNode(scope.row)">{{
                                state.nodesDic[scope.row.NodeId] || '选择节点' }}</a>
                            </p>
                            <p>
                                <span>打洞 : </span>
                                <a v-if="state.p2pOperatings[scope.row.RemoteMachineId]" href="javascript:;" class="a-line">
                                    <span>操作中.</span><el-icon size="14" class="loading"><Loading /></el-icon>
                                </a>
                                <a v-else href="javascript:;" class="a-line" @click="handlep2p(scope.row)">尝试打洞</a>
                            </p>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="54">
                    <template #default="scope">
                        <div>
                            <el-popconfirm v-if="hasTunnelRemove" confirm-button-text="确认" cancel-button-text="取消"
                                title="确定关闭此连接?" @confirm="handleDel(scope.row)">
                                <template #reference>
                                    <el-button type="danger" size="small"><el-icon>
                                            <Delete />
                                        </el-icon></el-button>
                                </template>
                            </el-popconfirm>
                        </div>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
    <el-dialog v-model="state.showNodes" :title="$t('server.relayTitle')" width="98%" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="600">
                <el-table-column property="Name" :label="$t('server.relayName')">
                    <template #default="scope">
                        <div>
                            <a :href="scope.row.Url" class="a-line blue" target="_blank">{{ scope.row.Name }}</a>
                            <a href="javascript:;" class="a-line">
                                <span v-if="(scope.row.AllowProtocol & 1) == 1">,tcp</span>
                                <span v-if="(scope.row.AllowProtocol & 2) == 2">,udp</span>
                            </a>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="MaxGbTotal" :label="$t('server.relayFlow')" width="140">
                    <template #default="scope">
                        <span v-if="scope.row.MaxGbTotal == 0">--</span>
                        <span v-else>
                            {{ (scope.row.MaxGbTotalLastBytes / 1024 / 1024 / 1024).toFixed(2) }}GB / {{
                                scope.row.MaxGbTotal
                            }}GB
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidth" :label="$t('server.relaySpeed')" width="80">
                    <template #default="scope">
                        <span v-if="scope.row.MaxBandwidth == 0">--</span>
                        <span v-else>{{ scope.row.MaxBandwidth }}Mbps</span>
                    </template>
                </el-table-column>
                <el-table-column property="MaxBandwidthTotal"
                    :label="`${$t('server.relaySpeed2')}/${$t('server.relaySpeed1')}`" width="120">
                    <template #default="scope">
                        <span>
                            <span>{{ scope.row.BandwidthRatio }}Mbps</span>
                            <span>/</span>
                            <span v-if="scope.row.MaxBandwidthTotal == 0">--</span>
                            <span v-else>{{ scope.row.MaxBandwidthTotal }}Mbps</span>
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionRatio" :label="$t('server.relayConnection')" width="80">
                    <template #default="scope">
                        <span><strong>{{ scope.row.ConnectionRatio }}</strong>/{{ scope.row.MaxConnection }}</span>
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
                <el-table-column property="Oper" :label="$t('server.relayOper')" width="75">
                    <template #default="scope">
                        <el-dropdown size="small">
                            <div class="dropdown">
                                <span>{{ $t('server.relayUse') }}</span>
                                <el-icon class="el-icon--right">
                                    <ArrowDown />
                                </el-icon>
                            </div>
                            <template #dropdown>
                                <el-dropdown-menu>
                                    <el-dropdown-item v-if="(scope.row.AllowProtocol & 1) == 1" @click="handleConnect(scope.row.Id, 1)">{{$t('common.relay')}}TCP</el-dropdown-item>
                                    <el-dropdown-item v-if="(scope.row.AllowProtocol & 2) == 2" @click="handleConnect(scope.row.Id, 2)">{{$t('common.relay')}}UDP</el-dropdown-item>
                                </el-dropdown-menu>
                            </template>
                        </el-dropdown>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useConnections, useForwardConnections, useSocks5Connections, useTuntapConnections } from './connections';
import { Delete, Select, ArrowDown,Loading } from '@element-plus/icons-vue';
import { injectGlobalData } from '@/provide';
import { relayConnect, setRelaySubscribe } from '@/apis/relay';
import { useI18n } from 'vue-i18n';
import { useTunnel } from './tunnel';
import { tunnelConnect } from '@/apis/tunnel';
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { Delete, Select, ArrowDown,Loading },
    setup(props, { emit }) {

        const { t } = useI18n();
        const globalData = injectGlobalData();
        const hasTunnelRemove = computed(() => globalData.value.hasAccess('TunnelRemove'));

        const connections = useConnections();
        const forwardConnections = useForwardConnections();
        const tuntapConnections = useTuntapConnections();
        const socks5Connections = useSocks5Connections();
        const tunnel = useTunnel();
        const state = reactive({
            show: true,
            protocolTypes: { 1: 'tcp', 2: 'udp', 4: 'msquic' },
            types: { 0: '打洞', 1: '中继', 2: '节点' },
            transactions: { 'forward': '端口转发', 'tuntap': '虚拟网卡', 'socks5': '代理转发' },
            machineName: connections.value.currentName,
            currentRow: {},
            data: computed(() => {
                return [
                    forwardConnections.value.list[connections.value.current],
                    tuntapConnections.value.list[connections.value.current],
                    socks5Connections.value.list[connections.value.current],
                ].filter(c => !!c);
            }),
            showNodes: false,
            nodes: [],
            nodesDic: {},
            timer: 0,

            relayOperatings:tunnel.value.relayOperatings,
            p2pOperatings:tunnel.value.p2pOperatings,
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });
        const handleDel = (row) => {
            if (!hasTunnelRemove.value) {
                ElMessage.success('无权限');
                return;
            }
            row.removeFunc(row.RemoteMachineId).then(() => {
                ElMessage.success(t('common.oper'));
            }).catch(() => { });
        }

        const _setRelaySubscribe = () => {
            clearTimeout(state.timer);
            setRelaySubscribe().then((res) => {
                state.nodes = res;
                state.nodesDic = res.reduce((a, b) => {
                    a[b.Id] = b.Name;
                    return a;
                }, {});
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            }).catch(() => {
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            });
        }

        const handlep2p = (row)=>{
            tunnelConnect({
                ToMachineId:row.RemoteMachineId,
                TransactionId:row.TransactionId,
                DenyProtocols:row.TransactionId == 'tuntap' ? 4 : 2
            }).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch(()=>{ElMessage.success(t('common.operFail'));})
        }

        const handleNode = (row) => {
            state.currentRow = row;
            state.showNodes = true;
        }
        const handleConnect = (id, protocol) => {
            const json = {
                FromMachineId: globalData.value.config.Client.Id,
                TransactionId: state.currentRow.TransactionId,
                ToMachineId: state.currentRow.RemoteMachineId,
                NodeId: id,
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
            state, handleDel, hasTunnelRemove,handlep2p, handleNode, handleConnect
        }
    }
}
</script>
<style lang="stylus" scoped>

.head{padding-bottom:1rem}
.blue {
    color: #409EFF;
}
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