<template>
    <el-dialog :title="$t('status.flowRelay')" class="options-center" top="1vh" destroy-on-close v-model="state.show" width="680">
        <div>
            <div class="head">
                <el-input v-trim v-model="state.page.Key" placeholder="名字搜索"></el-input>
            </div>
            <el-table :data="state.list" stripe border size="small" width="100%" height="60vh" @sort-change="handleSort">
                <el-table-column prop="FromName" :label="$t('status.flowRelaySource')" width="120"></el-table-column>
                <el-table-column prop="ToName" :label="$t('status.flowRelayTarget')" width="120"></el-table-column>
                <el-table-column prop="SendtBytes" :label="$t('status.flowUpload')" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.SendtBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="DiffSendtBytes" :label="$t('status.flowUpload')" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.DiffSendtBytesText }}/s</span>
                    </template>
                </el-table-column>
                <el-table-column prop="ReceiveBytes" :label="$t('status.flowDownload')" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.ReceiveBytesText }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="DiffReceiveBytes" :label="$t('status.flowDownload')" sortable="custom">
                    <template #default="scope">
                        <span>{{ scope.row.DiffReceiveBytesText }}/s</span>
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
import { getRelayFlows } from '@/apis/flow';
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

        const _getRelayFlows = ()=>{
            clearTimeout(state.timer);
            getRelayFlows({
                Key: state.page.Key,
                Page:state.page.Page,
                PageSize:state.page.PageSize,
                Order:state.page.Order,
                OrderType:state.page.OrderType,
            }).then(res => {
                try{
                    state.page.Page = res.Page;
                    state.page.PageSize = res.PageSize;
                    state.page.Count = res.Count;

                    const list = [];
                    for(let i = 0; i < res.Data.length; i++ ){
                        const item = res.Data[i];
                        Object.assign(item,{
                            SendtBytesText:parseSpeed(item.SendtBytes),
                            ReceiveBytesText:parseSpeed(item.ReceiveBytes),
                            DiffSendtBytesText:parseSpeed(item.DiffSendtBytes),
                            DiffReceiveBytesText:parseSpeed(item.DiffReceiveBytes),
                        } );
                        list.push(item);
                    }
                    state.list = list;
                }catch(e){
                    console.log(e);
                }
                state.timer = setTimeout(_getRelayFlows,1000);
            }).catch((e)=>{
                state.timer = setTimeout(_getRelayFlows,1000);
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

        onMounted(()=>{
            _getRelayFlows();
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