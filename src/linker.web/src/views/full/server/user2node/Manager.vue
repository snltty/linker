<template>
    <el-dialog class="options-center" :title="$t('server.relayUser2Node')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
        <div class="head">
            <div class="search flex">
                <div><span>{{$t('server.relayUser2NodeUserId')}}</span> <el-input v-model="state.page.UserId" style="width:8rem" size="small" clearable @change="handleSearch" /></div>
                <div><span>{{$t('server.relayUser2NodeName')}}</span> <el-input v-model="state.page.Name" style="width:8rem" size="small" clearable @change="handleSearch" /></div>
                <div><span>{{$t('server.relayUser2NodeRemark')}}</span> <el-input v-model="state.page.Remark" style="width:8rem" size="small" clearable @change="handleSearch" /></div>
                <div>
                    <el-button size="small" @click="handleSearch()">
                        <el-icon><Search /></el-icon>
                    </el-button>
                </div>
                <div>
                    <el-button size="small" type="success" @click="handleAdd">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                </div>
            </div>
        </div>
        <el-table stripe :data="state.list.List" border size="small" width="100%">
            
            <el-table-column prop="Name" :label="$t('server.relayUser2NodeName')"></el-table-column>
            <el-table-column prop="Nodes" :label="$t('server.relayUser2NodeNodes')">
                <template #default="scope">
                    <span>{{ scope.row.Nodes.map(c=>state.nodes[c]).join(',') }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Remark" :label="$t('server.relayUser2NodeRemark')"></el-table-column>
            <el-table-column prop="AddTime" :label="`${$t('server.relayUser2NodeAddTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column fixed="right" prop="Oper" :label="$t('server.relayUser2NodeOper')" width="110">
                <template #default="scope">
                    <el-button size="small" @click="handleEdit(scope.row)">
                        <el-icon><EditPen /></el-icon>
                    </el-button>
                    <el-popconfirm :title="$t('server.relayUser2NodeDelConfirm')" @confirm="handleDel(scope.row)">
                        <template #reference>
                            <el-button type="danger" size="small">
                                <el-icon><Delete /></el-icon>
                            </el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <div class="t-c">
            <div class="page">
                <el-pagination small background layout="prev, pager, next" 
                    :page-size="state.page.Size" 
                    :total="state.list.Count" 
                    :pager-count="5"
                    :current-page="state.page.Page" @current-change="handlePageChange" />
            </div>
        </div>
    </div>
    </el-dialog>
    <Add v-if="state.showAdd" v-model="state.showAdd" @success="handleSearch"></Add>
</template>

<script>
import { computed, inject, onMounted, provide, reactive, ref, watch } from 'vue'
import { Delete,Plus,Search,Warning,EditPen } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import Add from './Add.vue';
import { user2NodeDel, user2NodePage } from '@/apis/relay';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Delete,Plus,Search ,EditPen,Add,Warning},
    setup(props,{emit}) {
        const {t} = useI18n();
        const nodes = inject('nodes');
        const state = reactive({
            nodes:computed(()=>nodes.value.reduce((json,item,index)=>{ json[item.Id] = item.Name; return json; },{})),
            page:{
                Page:1,
                Size:10,
                UserId:'',
                Name:'',
                Remark:''
            },
            list:{
                Page:1,
                Size:15,
                Count:0,
                List:[]
            },
            show:true,
            showAdd:false
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });


        const editState = ref({
        });
        provide('edit',editState);

        const handleAdd = ()=>{
            editState.value = {Id:0,Name:'',Nodes:[],Remark:'',UserId:''};
            state.showAdd = true;
        }
        const handleEdit = (row)=>{
            editState.value = row
            state.showAdd = true;
        }
        const handleSearch = ()=>{
            user2NodePage(state.page).then((res)=>{
                state.list = res;
            }).catch(()=>{})
        }
        const handlePageChange = (p)=>{
            state.page.Page = p;
            handleSearch();
        }
        const handleDel = (row)=>{
            user2NodeDel(row.Id).then((res)=>{
                handleSearch();
            }).catch(()=>{})
        }

        onMounted(()=>{
            handleSearch();
        })

        return {state,handleSearch,handlePageChange,handleDel,handleAdd,handleEdit}
    }
}
</script>
<style lang="stylus" scoped>
.head{
    .search{
        &>div{
            margin-right:1rem;
        }
    }
}
.page{
    padding:2rem 0;
    display:inline-block;
}
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>