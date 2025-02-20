<template>
    <div class="flow-wrap" v-if="config">
        <p>{{$t('status.flowOnline')}} 
            <a href="javascript:;" @click="state.showMap=true" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowOnline')}/${$t('status.flowOnline7Day')}`">{{state.overallOnline}}</a>
            <a href="javascript:;" @click="state.showAllMap=true" :title="`${$t('status.flowAllServer')}\r\n${$t('status.flowOnline')}/${$t('status.flowOnline7Day')}/${$t('status.flowServer')}`">{{ state.serverOnline }}</a>
        </p>
        <p>{{$t('status.flowUpload')}} <a href="javascript:;" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowAllSend')}`" @click="handleShow">{{state.overallSendtSpeed}}/s</a></p>
        <p>{{$t('status.flowDownload')}} <a href="javascript:;" :title="`${$t('status.flowThisServer')}\r\n${$t('status.flowAllReceive')}`" @click="handleShow">{{state.overallReceiveSpeed}}/s</a></p>
    </div>
    <el-dialog :title="state.time" destroy-on-close v-model="state.show" width="640">
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
                        <span>{{ scope.row.sendtSpeedText }}/s</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveBytes" :label="$t('status.flowDownload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveSpeed" :label="$t('status.flowDownload')" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveSpeedText }}/s</span>
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
    <ServerFlowMessenger :config="config" v-if="state.details.Messenger" v-model="state.details.Messenger"></ServerFlowMessenger>
    <ServerFlowSForward :config="config" v-if="state.details.SForward" v-model="state.details.SForward"></ServerFlowSForward>
    <ServerFlowRelay :config="config" v-if="state.details.Relay" v-model="state.details.Relay"></ServerFlowRelay>
    <OnlineMap :config="config" v-if="state.showMap" v-model="state.showMap"></OnlineMap>
    <OnlineAllMap :config="config" v-if="state.showAllMap" v-model="state.showAllMap"></OnlineAllMap>
</template>

<script>
import { getFlows } from '@/apis/flow';
import { computed, onMounted, onUnmounted, reactive } from 'vue';
import ServerFlowMessenger from './ServerFlowMessenger.vue';
import ServerFlowSForward from './ServerFlowSForward.vue';
import ServerFlowRelay from './ServerFlowRelay.vue';
import { injectGlobalData } from '@/provide';
import { useI18n } from 'vue-i18n';
import OnlineMap from './OnlineMap.vue';
import OnlineAllMap from './OnlineAllMap.vue';
export default {
    props:['config'],
    components:{ServerFlowMessenger,ServerFlowSForward,ServerFlowRelay,OnlineMap,OnlineAllMap},
    setup (props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const hasSForwardFlow = computed(()=>globalData.value.hasAccess('SForwardFlow')); 
        const hasRelayFlow = computed(()=>globalData.value.hasAccess('RelayFlow')); 
        const hasSigninFlow = computed(()=>globalData.value.hasAccess('SigninFlow')); 
        const state = reactive({
            show:false,
            timer:0,
            overallSendtSpeed: '0000.00KB',
            overallReceiveSpeed: '0000.00KB',
            overallOnline: '0/0',
            serverOnline: '',
            time:'',
            list:[],
            old:null,
            details:{
                Messenger:false,
                SForward:false,
                Relay:false,
            },
            showMap:false,
            showAllMap:false,
        });
        const handleShow = ()=>{
            state.show = true;
        }
        const handleShowDetail = (id)=>{
            state.details[id] = true;
        }
        const id2text = {
            'External':{text:t('status.flowWanPort'),detail:false},
            'RelayReport':{text:t('status.flowRelayNode'),detail:false},
            'Relay':{text:t('status.flowRelay'),detail:hasRelayFlow.value},
            'Messenger':{text:t('status.flowMessenger'),detail:hasSigninFlow.value},
            'SForward':{text:t('status.flowServerForward'),detail:hasSForwardFlow.value},
            'flow':{text:'',detail:false},
        };
        const _getFlows = ()=>{
            getFlows().then(res => {
                const old = state.old || res;
                if(res.Items['_']){
                    state.overallOnline = `${res.Items['_'].SendtBytes}/${res.Items['_'].ReceiveBytes}`;
                    delete res.Items['_'];
                }
                if(res.Items['flow'] && res.Items['flow'].ReceiveBytes>0){
                    const online = (BigInt(res.Items['flow'].ReceiveBytes) >> BigInt(32)).toString();
                    const total = (BigInt(res.Items['flow'].ReceiveBytes) & BigInt(0xffffffff)).toString();
                    const server = res.Items['flow'].SendtBytes;
                    state.serverOnline = `、${online}/${total}/${server}`;
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
                state.overallSendtSpeed = parseSpeed(sendtBytes-_sendtBytes);
                state.overallReceiveSpeed = parseSpeed(receiveBytes-_receiveBytes);

                state.time = `${res.Start}`;
                const list = [];
                for(let j in res.Items){
                    const item = res.Items[j];
                    const itemOld = old.Items[j];
                    const text = id2text[`${j}`] || {text:`Unknow${j}`,detail:false};
                    list.push({
                        id:j,
                        text:text.text,
                        detail:text.detail,

                        sendtBytes:item.SendtBytes,
                        sendtBytesText:parseSpeed(item.SendtBytes),

                        sendtSpeed:item.SendtBytes-itemOld.SendtBytes,
                        sendtSpeedText:parseSpeed(item.SendtBytes-itemOld.SendtBytes),

                        receiveBytes:item.ReceiveBytes,
                        receiveBytesText:parseSpeed(item.ReceiveBytes),

                        receiveSpeed:item.ReceiveBytes-itemOld.ReceiveBytes,
                        receiveSpeedText:parseSpeed(item.ReceiveBytes-itemOld.ReceiveBytes),
                    });
                }
                state.list = list.filter(c=>!!c.id);

                state.old = res;
                state.timer = setTimeout(_getFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getFlows,1000);
            });
        }
        const parseSpeed = (num) => {
            let index = 0;
            while (num >= 1024) {
                num /= 1024;
                index++;
            }
            return `${num.toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}`;
        }

        onMounted(()=>{
            _getFlows();
            //_getTunnelRecords();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            hasSForwardFlow,config:props.config,state,handleShow,handleShowDetail
        }
    }
}
</script>

<style lang="stylus" scoped>
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