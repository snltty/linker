<template>
    <div>
        <div class="head">
            <el-button size="small" @click="getData"><el-icon><Search /></el-icon></el-button>
        </div>
        <el-table :data="state.request.List" stripe border size="small" width="100%">
            <el-table-column prop="Addr" label="IP"></el-table-column>
            <el-table-column prop="Oper" :label="$t('server.denyOper')" width="60">
                <template #default="scope">
                    <el-button type="danger" size="small" @click="handleDeny(scope.row)">
                        <el-icon><Remove /></el-icon>
                    </el-button>
                </template>
            </el-table-column>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,prev,pager, next" :total="state.request.Count"
                    :page-size="state.request.Size" :current-page="state.request.Page" @current-change="handlePageChange"/>
            </div>
        </div>
        <Add v-model="state.show" v-if="state.show" :data="state.current" :type="type"></Add>
    </div>
</template>

<script>
import {  relayMasters } from '@/apis/relay';
import { sforwardMasters } from '@/apis/sforward';
import {  onMounted, reactive } from 'vue';
import { Remove,Search } from '@element-plus/icons-vue';
import Add from './Add.vue';
export default {
    props: ['type','data'],
    components:{Remove,Search,Add},
    setup (props,{emit}) {

        const getDataFn = props.type == 'relay' ? relayMasters : sforwardMasters;
        const state = reactive({
            request:{
                NodeId:props.data.NodeId,
                Page: 1,
                Size: 10,
                Count:0,
                List:[]
            },
            show: false,
            current:{},
        });

        const getData = ()=>{
            getDataFn({
                NodeId:props.data.NodeId,
                Page: state.request.Page,
                Size:state.request.Size ,
            }).then(res=>{
                state.request.Page = res.Page;
                state.request.Size = res.Size;
                state.request.Count = res.Count;
                state.request.List = res.List;
            })
        }

        const handlePageChange = (page)=>{
            if (page) {
               state.request.Page = page;
               getData();
            }
        }
        const handleDeny = (row)=>{
            state.current = {
                Id:0,
                NodeId:props.data.NodeId,
                Str:`${row.Addr.split(':')[0]}/32`
            };
            state.show = true;
        }

        onMounted(()=>{
            getData();
        });
        

        return {state,getData,handlePageChange,handleDeny}
    }
}
</script>

<style lang="stylus" scoped>
.head{
    padding-bottom:1rem;
    .el-input{width:20rem}
}
.page{padding-top:1rem}
.page-wrap{
    display:inline-block;
}
</style>