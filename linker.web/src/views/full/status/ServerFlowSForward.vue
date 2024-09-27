<template>
    <el-dialog title="内网穿透流量" class="options-center" top="1vh" destroy-on-close v-model="state.show" width="680">
        <div>
            <div class="head">
                <el-input v-model="state.page.Key" placeholder="域名/端口搜索"></el-input>
            </div>
            <el-table :data="state.list" stripe border size="small" width="100%" height="60vh" @sort-change="handleSort">
                <el-table-column prop="id" label="域名/端口" width="200"></el-table-column>
                <el-table-column prop="sendtBytes" label="已上传" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.sendtBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="sendtSpeed" label="上传速度" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.sendtSpeedText }}/s</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveBytes" label="已下载" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.receiveBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="receiveSpeed" label="下载速度" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.receiveSpeedText }}/s</span>
                    </template>
                </el-table-column>
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
import { getSForwardFlows } from '@/apis/flow';
import { onMounted, onUnmounted, reactive, watch } from 'vue';

export default {
    props: ['modelValue','config'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {
        
        const state = reactive({
            show:true,
            timer:0,
            list:[],
            page:{
                Key:'',
                Page:1,
                PageSize:15,
                Count:0,
                Order:1,
                OrderType:0
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _getSForwardFlows = ()=>{
            getSForwardFlows({
                Key: state.page.Key,
                Page:state.page.Page,
                PageSize:state.page.PageSize,
                Order:state.page.Order,
                OrderType:state.page.OrderType,
            }).then(res => {
                state.page.Page = res.Page;
                state.page.PageSize = res.PageSize;
                state.page.Count = res.Count;

                const list = [];

                const keys = Object.keys(res.Data);
                console.log(res.Data);
                for(let i = 0; i < keys.length; i++ ){
                    const item = res.Data[keys[i]];
                    list.push({
                        id:keys[i],
                        sendtBytes:item.SendtBytes,
                        sendtBytesText:parseSpeed(item.SendtBytes),

                        sendtSpeed:item.DiffSendtBytes,
                        sendtSpeedText:parseSpeed(item.DiffSendtBytes),

                        receiveBytes:item.ReceiveBytes,
                        receiveBytesText:parseSpeed(item.ReceiveBytes),

                        receiveSpeed:item.DiffReceiveBytes,
                        receiveSpeedText:parseSpeed(item.DiffReceiveBytes),
                    });
                }
                
                state.list = list.filter(c=>!!c.id);

                state.old = res;
                state.timer = setTimeout(_getSForwardFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getSForwardFlows,1000);
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
            const order = {'sendtBytes':1,'sendtSpeed':2,'receiveBytes':3,'receiveSpeed':4}[a.prop];
            state.page.Order = order;
            state.page.OrderType = orderType;
        }

        onMounted(()=>{
            _getSForwardFlows();
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