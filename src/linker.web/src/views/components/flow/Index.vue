<template>
    <el-dialog :title="state.time" destroy-on-close v-model="flow.count" width="640">
        <div>
            <el-table :data="state.list" border size="small" width="100%">
                <el-table-column prop="text" :label="$t('status.flowType')"></el-table-column>
                <el-table-column prop="sendtBytes" :label="$t('status.flowUpload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.sendtBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="sendtSpeed" :label="$t('status.flowUpload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.sendtSpeedText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveBytes" :label="$t('status.flowDownload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveSpeed" :label="$t('status.flowDownload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveSpeedText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="oper" :label="$t('status.flowOper')" width="70">
                    <template #default="scope">
                        <el-button v-if="scope.row.detail" size="small" @click="handleShowDetail(scope.row.id)">{{$t('status.flowDetail')}}</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
    <ServerFlowMessenger :config="config" :machineId="flow.machineId" v-if="state.details.Messenger" v-model="state.details.Messenger"></ServerFlowMessenger>
    <ServerFlowSForward :config="config" :machineId="flow.machineId" v-if="state.details.SForward" v-model="state.details.SForward"></ServerFlowSForward>
    <ServerFlowForward :config="config" :machineId="flow.machineId" v-if="state.details.Forward" v-model="state.details.Forward"></ServerFlowForward>
    <ServerFlowSocks5 :config="config" :machineId="flow.machineId" v-if="state.details.Socks5" v-model="state.details.Socks5"></ServerFlowSocks5>
    <ServerFlowTunnel :config="config" :machineId="flow.machineId" v-if="state.details.Tunnel" v-model="state.details.Tunnel"></ServerFlowTunnel>
    <ServerFlowRelay :config="config" v-if="state.details.Relay" v-model="state.details.Relay"></ServerFlowRelay>
    <OnlineMap :config="config" v-if="flow.map" v-model="flow.map"></OnlineMap>
    <OnlineAllMap :config="config" v-if="flow.allmap" v-model="flow.allmap"></OnlineAllMap>
</template>

<script>
import { getFlows } from '@/apis/flow';
import { computed,  onMounted, onUnmounted, reactive } from 'vue';
import ServerFlowMessenger from './ServerFlowMessenger.vue';
import ServerFlowSForward from './ServerFlowSForward.vue';
import ServerFlowForward from './ServerFlowForward.vue';
import ServerFlowSocks5 from './ServerFlowSocks5.vue';
import ServerFlowTunnel from './ServerFlowTunnel.vue';
import ServerFlowRelay from './ServerFlowRelay.vue';
import { injectGlobalData } from '@/provide';
import { useI18n } from 'vue-i18n';
import OnlineMap from './OnlineMap.vue';
import OnlineAllMap from './OnlineAllMap.vue';
import { useFlow } from './flow';
export default {
    props:['config','title'],
    components:{ServerFlowMessenger,ServerFlowSForward,ServerFlowForward,ServerFlowSocks5,ServerFlowTunnel,ServerFlowRelay,OnlineMap,OnlineAllMap},
    setup (props,{emit}) {

        const flow = useFlow();

        const {t} = useI18n();
        const globalData = injectGlobalData();
        const hasSForwardFlow = computed(()=>globalData.value.hasAccess('SForwardFlow')); 
        const hasRelayFlow = computed(()=>globalData.value.hasAccess('RelayFlow')); 
        const hasSigninFlow = computed(()=>globalData.value.hasAccess('SigninFlow')); 
        const hasForwardFlow = computed(()=>globalData.value.hasAccess('ForwardFlow')); 
        const hasSocks5Flow = computed(()=>globalData.value.hasAccess('Socks5Flow')); 
        const hasTunnelFlow = computed(()=>globalData.value.hasAccess('TunnelFlow')); 
        const state = reactive({
            timer:0,
            time:'',
            list:[],
            old:null,
            details:{
                Messenger:false,
                SForward:false,
                Forward:false,
                Socks5:false,
                Tunnel:false,
                Relay:false,
            }
        });

        const handleShowDetail = (id)=>{
            state.details[id] = true;
        }
        const id2text = {
            'External':{text:t('status.flowWanPort'),detail:false,format:true,suffix:'/s'},
            'RelayReport':{text:t('status.flowRelayNode'),detail:false,format:true,suffix:'/s'},
            'Relay':{text:t('status.flowRelay'),detail:hasRelayFlow.value,format:true,suffix:'/s'},
            'Messenger':{text:t('status.flowMessenger'),detail:hasSigninFlow.value,format:true,suffix:'/s'},
            'SForward':{text:t('status.flowServerForward'),detail:hasSForwardFlow.value,format:true,suffix:'/s'},
            'flow':{text:'',detail:false},
            'Forward':{text:t('status.flowForward'),detail:hasForwardFlow.value,format:true,suffix:'/s'},
            'Socks5':{text:t('status.flowSocks5'),detail:hasSocks5Flow.value,format:true,suffix:'/s'},
            'Tunnel':{text:t('status.flowTunnel'),detail:hasTunnelFlow.value},
        };
        const _getFlows = ()=>{
            clearTimeout(state.timer);
            getFlows(flow.value.machineId).then(res => {
                const old = state.old || res;
                if(res.Items['_']){
                    flow.value.overallOnline = `${res.Items['_'].SendtBytes}/${res.Items['_'].ReceiveBytes}`;
                    delete res.Items['_'];
                }
                if(res.Items['flow'] && res.Items['flow'].ReceiveBytes>0){
                    const online = (BigInt(res.Items['flow'].ReceiveBytes) >> BigInt(32)).toString();
                    const total = (BigInt(res.Items['flow'].ReceiveBytes) & BigInt(0xffffffff)).toString();
                    const server = res.Items['flow'].SendtBytes;
                    flow.value.serverOnline = `、${online}/${total}/${server}`;
                    delete res.Items['flow'];
                }

                let _receiveBytes = 0,_sendtBytes = 0,receiveBytes = 0,sendtBytes = 0;
                for(let j in old.Items){
                    _receiveBytes+=old.Items[j].ReceiveBytes;
                    _sendtBytes+=old.Items[j].SendtBytes;
                }
                for(let j in res.Items){
                    receiveBytes+=res.Items[j].ReceiveBytes;
                    sendtBytes+=res.Items[j].SendtBytes;
                }
                flow.value.overallSendtSpeed = parseSpeed(sendtBytes-_sendtBytes,true,'/s');
                flow.value.overallReceiveSpeed = parseSpeed(receiveBytes-_receiveBytes,true,'/s');

                state.time = `[${props.title}]${res.Start}`;
                const list = [];
                for(let j in res.Items){
                    const item = res.Items[j];
                    const itemOld = old.Items[j];
                    const text = id2text[`${j}`]|| {text:`Unknow${j}`,detail:false};
                    if(!text.text) continue;

                    list.push({
                        id:j,
                        text:text.text,
                        detail:text.detail,

                        sendtBytes:item.SendtBytes,
                        sendtBytesText:parseSpeed(item.SendtBytes,text.format,''),

                        sendtSpeed:item.SendtBytes-itemOld.SendtBytes,
                        sendtSpeedText:parseSpeed(item.SendtBytes-itemOld.SendtBytes,text.format,text.suffix),

                        receiveBytes:item.ReceiveBytes,
                        receiveBytesText:parseSpeed(item.ReceiveBytes,text.format,''),

                        receiveSpeed:item.ReceiveBytes-itemOld.ReceiveBytes,
                        receiveSpeedText:parseSpeed(item.ReceiveBytes-itemOld.ReceiveBytes,text.format,text.suffix),
                    });
                }
                state.list = list.filter(c=>!!c.id);

                state.old = res;
                state.timer = setTimeout(_getFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getFlows,1000);
            });
        }
        const parseSpeed = (num,format,suffix) => {
            if(format === undefined) return num;
            let index = 0;
            while (num >= 1024) {
                num /= 1024;
                index++;
            }
            return `${num.toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}${suffix}`;
        }

        onMounted(()=>{
            _getFlows();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            config:props.config,state,flow,handleShowDetail
        }
    }
}
</script>

<style lang="stylus" scoped>
html.dark .flow-wrap{
    background-color: #242526;
    border-color: #575c61;
}
.flow-wrap{
    padding:.4rem;
    font-weight:bold;position:absolute;right:1rem;bottom:80%;
    border:1px solid #ddd;
    background-color:#fff;
    z-index :9
    &>a,&>p{
        line-height:normal;
        white-space: nowrap;
        display:block;
    }
}
</style>