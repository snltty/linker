<template>
    <a v-if="config" href="javascript:;" title="linker服务端网速，点击查看详细信息" @click="handleShow">
        <p>上传 {{state.overallSendtSpeed}}</p>
        <p>下载 {{state.overallReceiveSpeed}}</p>
    </a>
    <el-dialog class="options-center" :title="state.time" destroy-on-close v-model="state.show" center  width="580" top="1vh">
        <div>
            <el-table :data="state.list" border size="small" width="100%" height="60vh">
                <el-table-column prop="id" label="信标id"></el-table-column>
                <el-table-column prop="sendtBytes" label="总上传字节" sortable></el-table-column>
                <el-table-column prop="sendtText" label="上传速度" sortable></el-table-column>
                <el-table-column prop="receiveBytes" label="总下载字节" sortable></el-table-column>
                <el-table-column prop="receiveText" label="下载速度" sortable></el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>

<script>
import { getFlows } from '@/apis/server';
import { onMounted, onUnmounted, reactive } from 'vue';

export default {
    props:['config'],
    setup (props) {
        
        const state = reactive({
            show:true,
            timer:0,
            overallSendtSpeed: '0000.00KB/s',
            overallReceiveSpeed: '0000.00KB/s',
            time:'',
            list:[],
            old:null
        });
        const handleShow = ()=>{
            state.show = true;
        }

        const id2text = {
            'External':'外网端口',
            'Relay':'中继',
            'Messenger':'信标总计',
            '0':'登入信标',
            '1':'客户端列表',
            '2':'客户端删除',
            '3':'客户端改名',
            '4':'客户端改名转发',
            '7':'服务器版本',
            '8':'客户端搜索ids',
            '9':'客户端id列表',
            '10':'客户端排序',
            '11':'客户端在线',
            '12':'生成客户端id',
            '13':'登入信标V_1_3_1',
            '2001':'外网端口',
            '2002':'外网端口转发',
            '2003':'开始打洞',
            '2004':'开始打洞转发',
            '2005':'打洞失败',
            '2006':'打洞失败转发',
            '2007':'打洞成功',
            '2008':'打洞成功转发',
            '2009':'隧道配置',
            '2010':'隧道配置转发',
            '2011':'隧道同步',
            '2012':'隧道同步转发',
            '2101':'收到中继',
            '2102':'收到中继转发',
            '2103':'中继请求',
            '2104':'中继请求转发',
            '2105':'中继测试',
            '2200':'运行网卡',
            '2201':'运行网卡转发',
            '2202':'停止网卡',
            '2203':'停止网卡转发',
            '2204':'更新网卡',
            '2205':'更新网卡转发',
            '2206':'同步网卡',
            '2207':'同步网卡转发',
            '2301':'添加穿透',
            '2302':'移除穿透',
            '2303':'收到TCP穿透',
            '2304':'收到UDP穿透',
            '2305':'获取穿透列表',
            '2306':'被获取穿透列表',
            '2401':'测试端口转发',
            '2402':'被测试端口转发',
            '2403':'获取端口转发列表',
            '2404':'被获取端口转发列表',
            '2503':'被获取权限',
            '2504':'获取权限转发',
            '2505':'更新权限',
            '2506':'更新权限转发',
            '2507':'同步密钥',
            '2508':'同步密钥转发',
            '2509':'同步服务器',
            '2510':'同步服务器转发',
            '2601':'更新信息转发',
            '2602':'更新信息',
            '2603':'确认更新转发',
            '2604':'更新转发',
            '2605':'重启',
            '2606':'重启转发',
            '2607':'服务器更新信息',
            '2608':'确认服务器更新',
            '2609':'服务器重启',
        }
        const _getFlows = ()=>{
            getFlows().then(res => {
                const old = state.old || res;

                let _receiveBytes = 0,_sendtBytes = 0,receiveBytes = 0,sendtBytes = 0;
                for(let j in old.Resolvers){
                    _receiveBytes+=old.Resolvers[j].ReceiveBytes;
                    _sendtBytes+=old.Resolvers[j].SendtBytes;
                }
                for(let j in res.Resolvers){
                    receiveBytes+=res.Resolvers[j].ReceiveBytes;
                    sendtBytes+=res.Resolvers[j].SendtBytes;
                }
                state.overallSendtSpeed = parseSpeed(sendtBytes-_sendtBytes);
                state.overallReceiveSpeed = parseSpeed(receiveBytes-_receiveBytes);

                state.time = `linker 启动于 ${res.Start} 至今`;
                const list = [];
                for(let j in res.Resolvers){
                    list.push({
                        id:id2text[`${j}`],
                        sendtBytes:res.Resolvers[j].ReceiveBytes,
                        sendtSpeed:res.Resolvers[j].ReceiveBytes-old.Resolvers[j].ReceiveBytes,
                        sendtText:parseSpeed(res.Resolvers[j].SendtBytes-old.Resolvers[j].SendtBytes),
                        receiveBytes:res.Resolvers[j].ReceiveBytes,
                        receiveSpeed:res.Resolvers[j].ReceiveBytes-old.Resolvers[j].ReceiveBytes,
                        receiveText:parseSpeed(res.Resolvers[j].ReceiveBytes-old.Resolvers[j].ReceiveBytes),
                    });
                }
                for(let j in res.Messangers){
                    list.push({
                        id:id2text[`${j}`],
                        sendtBytes:res.Messangers[j].SendtBytes,
                        sendtSpeed:res.Messangers[j].SendtBytes-old.Messangers[j].SendtBytes,
                        sendtText:parseSpeed(res.Messangers[j].SendtBytes-old.Messangers[j].SendtBytes),
                        receiveBytes:res.Messangers[j].ReceiveBytes,
                        receiveSpeed:res.Messangers[j].ReceiveBytes-old.Messangers[j].ReceiveBytes,
                        receiveText:parseSpeed(res.Messangers[j].ReceiveBytes-old.Messangers[j].ReceiveBytes),
                    });
                }
                state.list = list;

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
            return `${num.toFixed(2)}${['B/s', 'KB/s', 'MB/s', 'GB/s', 'TB/s'][index]}`;
        }

        onMounted(()=>{
            _getFlows();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            config:props.config,state,handleShow
        }
    }
}
</script>

<style lang="stylus" scoped>
a{
    font-weight:bold;position:absolute;right:1rem;bottom:90%;
    border:1px solid #ddd;
    background-color:#fff;
    p{
        line-height:normal;
        white-space: nowrap;
    }
}
</style>