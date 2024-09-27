<template>
    <a v-if="config" href="javascript:;" title="linker服务端网速，点击查看详细信息" @click="handleShow">
        <p>上传 {{state.overallSendtSpeed}}/s</p>
        <p>下载 {{state.overallReceiveSpeed}}/s</p>
    </a>
    <el-dialog :title="state.time" destroy-on-close v-model="state.show" width="540">
        <div>
            <el-table :data="state.list" border size="small" width="100%">
                <el-table-column prop="text" label="类别" width="80"></el-table-column>
                <el-table-column prop="sendtBytes" label="已上传" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.sendtBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="sendtSpeed" label="上传速度" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.sendtSpeedText }}/s</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveBytes" label="已下载" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveSpeed" label="下载速度" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.receiveSpeedText }}/s</span>
                    </template>
                </el-table-column>
                <el-table-column prop="oper" label="操作" width="64">
                    <template #default="scope">
                        <el-button v-if="scope.row.detail" size="small" @click="handleShowDetail(scope.row.id)">详情</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
    <ServerFlowMessenger :config="config" v-if="state.details.Messenger" v-model="state.details.Messenger"></ServerFlowMessenger>
    <ServerFlowSForward :config="config" v-if="state.details.SForward" v-model="state.details.SForward"></ServerFlowSForward>
</template>

<script>
import { getFlows } from '@/apis/flow';
import { onMounted, onUnmounted, reactive } from 'vue';
import ServerFlowMessenger from './ServerFlowMessenger.vue';
import ServerFlowSForward from './ServerFlowSForward.vue';
export default {
    props:['config'],
    components:{ServerFlowMessenger,ServerFlowSForward},
    setup (props) {
        
        const state = reactive({
            show:false,
            timer:0,
            overallSendtSpeed: '0000.00KB',
            overallReceiveSpeed: '0000.00KB',
            time:'',
            list:[],
            old:null,
            details:{
                Messenger:false,
                SForward:false,
            }
        });
        const handleShow = ()=>{
            state.show = true;
        }
        const handleShowDetail = (id)=>{
            state.details[id] = true;
        }

        const id2text = {
            'External':{text:'外网端口',detail:false},
            'Relay':{text:'中继',detail:true},
            'Messenger':{text:'信标',detail:true},
            'SForward':{text:'内网穿透',detail:true},
        };
        const _getFlows = ()=>{
            getFlows().then(res => {
                const old = state.old || res;

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

                state.time = `从 ${res.Start}启动 至今`;
                const list = [];
                for(let j in res.Items){
                    const item = res.Items[j];
                    const itemOld = old.Items[j];
                    const text = id2text[`${j}`] || {text:'未知',detail:false};
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
            config:props.config,state,handleShow,handleShowDetail
        }
    }
}
</script>

<style lang="stylus" scoped>
a{
    font-weight:bold;position:absolute;right:1rem;bottom:90%;
    border:1px solid #ddd;
    background-color:#fff;
    z-index :9
    p{
        line-height:normal;
        white-space: nowrap;
    }
}
</style>