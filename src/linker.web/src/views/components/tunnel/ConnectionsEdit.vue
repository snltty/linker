<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="$t('network.tunnel.title',[state.device.MachineName])" top="1vh" width="400">
        <div>
            <el-descriptions border size="small" :column="1" label-width="8rem" >
                <el-descriptions-item :label="$t('network.tunnel.target')" >
                    <div class="break-all">{{ state.connection.IPEndPoint }}</div>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('network.tunnel.trans')" >{{ state.transactions[state.connection.TransactionId] }}</el-descriptions-item>
                <el-descriptions-item :label="$t('network.tunnel.proto')">
                    <div v-if="state.connection.Connected">
                        <p>{{ state.connection.TransportName }}({{ state.protocolTypes[state.connection.ProtocolType] }}) - {{ state.types[state.connection.Type] }}</p>
                        <p>{{ state.connection.SendBufferRemainingText }} - {{ state.connection.RecvBufferRemainingText }}</p>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item label="SSL" >{{ state.connection.SSL }}</el-descriptions-item>
                
                <el-descriptions-item :label="$t('network.tunnel.up')">
                    <div>
                        <p><span>{{ state.connection.SendBytesText }}</span></p>
                    </div>
                </el-descriptions-item>
                 <el-descriptions-item :label="$t('network.tunnel.down')">
                    <div>
                        <p><span>{{ state.connection.ReceiveBytesText }}</span></p>
                    </div>
                </el-descriptions-item>
                
                <el-descriptions-item :label="$t('network.tunnel.relay')">
                    <div>
                        <a v-if="state.operating.relay" href="javascript:;" class="a-line">
                            <span>{{$t('network.tunnel.manual')}}</span><el-icon size="14" class="loading"><Loading /></el-icon>
                        </a>
                        <a v-else href="javascript:;" class="a-line" @click="handleNode">{{ state.nodesDic[state.connection.NodeId] || $t('network.tunnel.relay') }}</a>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('network.tunnel.pcp')">
                    <div>
                        <a v-if="state.operating.pcp" href="javascript:;" class="a-line">
                            <span>{{$t('network.tunnel.manual')}}</span><el-icon size="14" class="loading"><Loading /></el-icon>
                        </a>
                        <a v-else href="javascript:;" class="a-line" @click="handlePcp">{{$t('network.tunnel.pcp') }}</a>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('network.tunnel.p2p')">
                    <div>
                        <a v-if="state.operating.hand" href="javascript:;" class="a-line">
                            <span>{{$t('network.tunnel.manual')}}</span><el-icon size="14" class="loading"><Loading /></el-icon>
                        </a>
                        <template v-else>
                            <a href="javascript:;" class="a-line" @click="handlep2p">{{$t('network.tunnel.p2p')}}</a>
                            <span class="mgl-1">{{$t('network.tunnel.auto')}}<el-icon v-if="state.operating.default" size="14" class="loading"><Loading /></el-icon></span>
                            <span class="mgl-1">{{$t('network.tunnel.back')}}<el-icon v-if="state.operating.back" size="14" class="loading"><Loading /></el-icon></span>
                            <span class="mgl-1">{{$t('network.tunnel.pcp')}}<el-icon v-if="state.operating.pcp" size="14" class="loading"><Loading /></el-icon></span>
                        </template>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('network.tunnel.delay')" >{{ state.connection.Delay }}</el-descriptions-item>
                 <el-descriptions-item :label="$t('common.oper')" >
                    <div>
                        <AccessShow value="TunnelRemove">
                            <el-popconfirm 
                            :confirm-button-text="$t('common.confirm')" 
                            :cancel-button-text="$t('common.cancel')"
                            :title="$t('common.closeSure')" @confirm="handleDel">
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
    <el-dialog v-model="state.showNodes" :title="$t('relay.title')" width="98%" top="2vh">
        <div>
            <el-table :data="state.nodes" size="small" border height="600">
                <el-table-column property="Name" :label="$t('relay.name')">
                    <template #default="scope">
                        <div>
                            <a :href="scope.row.Url" class="a-line blue" target="_blank">{{ scope.row.Name }}</a>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column property="ConnectionsRatio" :label="$t('relay.conn')" width="80">
                    <template #default="scope">
                        <span><strong>{{ scope.row.ConnectionsRatio }}</strong></span>
                    </template>
                </el-table-column>
                <el-table-column property="BandwidthEach" :label="$t('relay.speed')" width="140">
                    <template #default="scope">
                        <p>
                            <span>{{ scope.row.BandwidthRatio }}Mbps</span>
                            <span> / </span>
                            <span v-if="scope.row.BandwidthEach == 0">--</span>
                            <span v-else>{{ scope.row.BandwidthEach }}Mbps</span>
                        </p>
                    </template>
                </el-table-column>
                 <el-table-column property="DataEachMonth" :label="$t('relay.flow')" width="100">
                    <template #default="scope">
                        <span v-if="scope.row.DataEachMonth == 0">--</span>
                        <span v-else>
                            {{ (scope.row.DataRemain / 1024 / 1024 / 1024).toFixed(2) }}GB
                        </span>
                    </template>
                </el-table-column>
                <el-table-column property="Delay" :label="$t('relay.delay')" width="60">
                    <template #default="scope">
                        <span>{{ scope.row.Delay }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column property="Public" :label="$t('relay.public')" width="55">
                    <template #default="scope">
                        <el-switch disabled v-model="scope.row.Public" size="small" />
                    </template>
                </el-table-column>
                <el-table-column property="Oper" :label="$t('relay.use')" width="130">
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
import { pcpConnect } from '@/apis/pcp';
import { useDevice } from '../device/devices';
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { Delete, Select, ArrowDown,Loading },
    setup(props, { emit }) {

        const { t } = useI18n();
        const globalData = injectGlobalData();

        const connections = useConnections();
        const connection =  computed(() =>connections.value.device.hook_connection? connections.value.device.hook_connection[connections.value.transactionId] || {} :{});
        const device = useDevice();

        const state = reactive({
            show: true,
            protocolTypes: { 1: 'tcp', 2: 'udp' },
            types: { 0: t('network.tunnel.p2p'), 1: t('network.tunnel.relay'), 2: t('network.tunnel.pcp') },
            transactions: { 'forward': t('forward'), 'tuntap': t('tuntap'), 'socks5': t('socks5') },
            device: connections.value.device,
            transactionId: connections.value.transactionId,
            operating:computed(()=>connections.value.device.hook_operating?connections.value.device.hook_operating[connections.value.transactionId]:{}),
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
                ElMessage.success(t('common.opered'));
            }).catch(() => { });
        }

        const _setRelaySubscribe = () => {
            clearTimeout(state.timer);
            setRelaySubscribe().then((res) => {
                state.nodes = res.filter(c=>c.LastTicks < 15000);
                state.nodesDic = res.reduce((a, b) => {
                    a[b.NodeId] = b.Name;
                    return a;
                }, {});
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            }).catch(() => {
                state.timer = setTimeout(_setRelaySubscribe, 1000);
            });
        }

        const filterProfiles = (profiles)=>{
            return profiles.filter(c=>c.Disabled == false);
        }
        const getFec = ()=>{
            try{
                const self = filterProfiles(device.page.List[0].hook_tuntap.FecProfile);
                if(self.length > 0){
                    return self
                }
                const other = filterProfiles(connections.value.device.hook_tuntap.FecProfile);
                if(other.length > 0){
                    return other
                }
            }catch(e){
            }
            return []
        }
        const getConfigures = ()=>{
            return {
                "fec":JSON.stringify(getFec())
            }
        }
        const handlep2p = ()=>{
            tunnelConnect({
                ToMachineId:state.device.MachineId,
                TransactionId:state.transactionId,
                DenyProtocols:state.transactionId == 'tuntap' ? 4 : 2,
                Configures:getConfigures()
            }).then(()=>{
                ElMessage.success(t('common.opered'));
            }).catch(()=>{ElMessage.success(t('common.operFail'));})
        }
        const handlePcp = ()=>{
            pcpConnect({
                ToMachineId:state.device.MachineId,
                TransactionId:state.transactionId,
                Configures:getConfigures()
            }).then(()=>{
                ElMessage.success(t('common.opered'));
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
                Protocol: protocol,
                Configures:getConfigures()
            };
            relayConnect(json).then(() => {ElMessage.success(t('common.opered')); }).catch(() => {ElMessage.success(t('common.operFail')); });
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
            state, handleDel,handlep2p, handleNode, handleConnect,handlePcp
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