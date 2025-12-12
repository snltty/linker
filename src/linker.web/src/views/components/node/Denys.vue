<template>
    <div>
        <div class="head">
            <el-input v-model="state.request.Str" size="small" clearable></el-input>
            <el-button class="mgl-1" size="small" @click="getData"><el-icon><Search /></el-icon></el-button>
            <el-button  type="success" size="small" @click="handleAdd"><el-icon><Plus /></el-icon></el-button>
        </div>
        <el-table :data="state.request.List" stripe border size="small" width="100%">
            <el-table-column prop="Str" :label="$t('server.denyStr')"></el-table-column>
            <el-table-column prop="Remark" :label="$t('server.denyRemark')"></el-table-column>
            <el-table-column prop="Oper" :label="$t('server.denyOper')" width="60">
                <template #default="scope">
                    <el-popconfirm :confirm-button-text="$t('common.confirm')" :cancel-button-text="$t('common.cancel')" :title="$t('server.denyDel')"
                        @confirm="handleDel(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="small"><el-icon><Delete /></el-icon></el-button>
                        </template>
                    </el-popconfirm>
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
import {  relayDenys, relayDenysDel} from '@/apis/relay';
import { sforwardDenys, sforwardDenysDel } from '@/apis/sforward';
import { onActivated, onMounted, reactive } from 'vue';
import { Delete,Plus,Search } from '@element-plus/icons-vue';
import Add from './Add.vue';
export default {
    props: ['type','data'],
    components:{Delete,Add,Plus,Search},
    setup (props,{emit}) {

        const getDataFn = props.type == 'relay' ? relayDenys : sforwardDenys;
        const delFn = props.type == 'relay' ? relayDenysDel : sforwardDenysDel;
        const state = reactive({
            request:{
                NodeId:props.data.NodeId,
                Page: 1,
                Size: 10,
                Count:0,
                Str:'',
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
                Str:state.request.Str ,
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
        const handleDel = (row)=>{
            delFn({
                Id:row.Id,
                NodeId:props.data.NodeId,
            }).then(()=>{
                getData();
            }).catch(()=>{});
        }
        const handleAdd = ()=>{
            state.current = {
                NodeId:props.data.NodeId,
                Str:'',
                Id:0,
                Remark:''
            };
            state.show = true;
        }

        onMounted(()=>{
            getData();
        });
        onActivated(()=>{
            getData();
        });
        

        return {state,getData,handlePageChange,handleDel,handleAdd}
    }
}
</script>

<style lang="stylus" scoped>
.head{
    padding-bottom:1rem;
    .el-input{width:10rem}
}
.page{padding-top:1rem}
.page-wrap{
    display:inline-block;
}
</style>