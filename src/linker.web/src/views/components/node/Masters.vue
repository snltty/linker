<template>
    <div>
        <el-table :data="state.request.List" stripe border size="small" width="100%">
            <el-table-column prop="Addr" label="IP"></el-table-column>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,prev,pager, next" :total="state.request.Count"
                    :page-size="state.request.Size" :current-page="state.request.Page" @current-change="handlePageChange"/>
            </div>
        </div>
    </div>
</template>

<script>
import { relayMasters } from '@/apis/relay';
import { sforwardMasters } from '@/apis/sforward';
import { onMounted, reactive } from 'vue';

export default {
    props: ['type','data'],
    setup (props,{emit}) {

        const getDataFn = props.type == 'relay' ? relayMasters : sforwardMasters;
        const state = reactive({
            request:{
                Page: 1,
                Size: 10,
                Count:0,
                List:[]
            }
        });

        const getData = ()=>{
            getDataFn(state.request).then(res=>{
                state.request.Page = res.Page;
                state.request.Size = res.Size;
                state.request.Count = res.Count;
                state.request.List = res.List;
            })
        }

        const handlePageChange = (page)=>{
            if (page) {
               state.request.Page = page;
            }
            getData();
        }

        onMounted(()=>{
            getData();
        });
        

        return {state,handlePageChange}
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