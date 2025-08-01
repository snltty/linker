<template>
    <el-dialog :title="$t('status.flowTunnel')" class="options-center" top="1vh" destroy-on-close v-model="state.show" width="90%">
        <div>
            <el-table :data="state.list" stripe border size="small" width="100%" height="60vh" @sort-change="handleSort">
                <el-table-column prop="Key" :label="$t('status.flowMachineName')" width="100">
                    <template #default="scope">
                        <span>{{ state.names[scope.row.Key] || 'unknow' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="TransitionId" :label="$t('status.flowTransitionId')">
                    <template #default="scope">
                        <span>{{ state.transitions[scope.row.TransitionId] || 'unknow' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="Direction" :label="$t('status.flowDirection')">
                    <template #default="scope">
                        <span>{{ state.dirs[scope.row.Direction] || 'unknow' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="Type" :label="$t('status.flowType')">
                    <template #default="scope">
                        <span>{{ state.types[scope.row.Type] || 'unknow' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="Mode" :label="$t('status.flowMode')">
                    <template #default="scope">
                        <span>{{ state.modes[scope.row.Mode] || 'unknow' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="ReceiveBytes" :label="$t('status.flowNum')" width="80"></el-table-column>
            </el-table>
            <div class="page t-c">
                <div class="page-wrap">
                    <el-pagination small background layout="total,prev,pager, next" :total="state.page.Count"
                        :page-size="state.page.PageSize" :current-page="state.page.Page" @current-change="handlePageChange"/>
                </div>
            </div>
        </div>
    </el-dialog>
</template>

<script>
import { getTunnelFlows } from '@/apis/flow';
import { getSignInNames } from '@/apis/signin';
import { onMounted, onUnmounted, reactive, watch } from 'vue';

export default {
    props: ['modelValue','config','machineId'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {
        
        const state = reactive({
            show:true,
            timer:0,
            names:{},
            list:[],
            page:{
                Page:1,
                PageSize:15,
                Count:0,
                Order:1,
                OrderType:0
            },
            transitions:{
                'socks5':'代理',
                'forward':'端口转发',
                'tuntap':'虚拟网卡',
            },
            dirs:['正向','反向'],
            types:['打洞','中继','节点'],
            modes:['客户端','服务端']
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _getTunnelFlows = ()=>{
            clearTimeout(state.timer);
            getTunnelFlows({
                Page:state.page.Page,
                PageSize:state.page.PageSize,
                Order:state.page.Order,
                OrderType:state.page.OrderType,
                MachineId:props.machineId
            }).then(res => {
                try{
                    state.page.Page = res.Page;
                    state.page.PageSize = res.PageSize;
                    state.page.Count = res.Count;

                    const list = [];
                    for(let i = 0; i < res.Data.length; i++ ){
                        const item = res.Data[i];
                        Object.assign(item,{
                            SendtBytesText:(item.SendtBytes),
                            ReceiveBytesText:(item.ReceiveBytes),
                            DiffSendtBytesText:(item.DiffSendtBytes),
                            DiffReceiveBytesText:(item.DiffReceiveBytes),
                        } );
                        list.push(item);
                    }
                    state.list = list;
                }catch(e){
                    console.log(e);
                }
                state.timer = setTimeout(_getTunnelFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getTunnelFlows,1000);
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

        const handlePageChange = (page)=>{
            if (page) {
               state.page.Page = page;
            }
        }
        const handleSort = (a)=>{
            const orderType = {'ascending':1,'descending':0}[a.order];
            const order = {'SendtBytes':1,'DiffSendtBytes':2,'ReceiveBytes':3,'DiffReceiveBytes':4}[a.prop];
            state.page.Order = order;
            state.page.OrderType = orderType;
        }

        const _getNames = ()=>{
            getSignInNames().then(res=>{
                state.names = res.reduce((json,item,index)=>{ json[item.MachineId] = item.MachineName;return json; },{});
            });
        }

        onMounted(()=>{
            _getTunnelFlows();
            _getNames();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            config:props.config,state,handlePageChange,handleSort
        }
    }
}
</script>

<style lang="stylus" scoped>
.head{
    padding-bottom:1rem;
    text-align:center;
    .el-input{width:20rem}
}
.page{padding-top:1rem}
.page-wrap{
    display:inline-block;
}
</style>