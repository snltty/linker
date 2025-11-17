<template>
    <el-dialog class="options-center" :title="$t('server.wlist')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
        <div class="head">
            <div class="search flex">
                <div><span>{{$t('server.wlistName')}}</span> <el-input v-trim v-model="state.page.Name" style="width:8rem" size="small" clearable @change="handleSearch" /></div>
                <div><span>{{$t('server.wlistRemark')}}</span> <el-input v-trim v-model="state.page.Remark" style="width:8rem" size="small" clearable @change="handleSearch" /></div>
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
            
            <el-table-column prop="Name" :label="$t('server.wlistName')"></el-table-column>
            <el-table-column prop="Nodes" :label="$t(`server.wlistNodes`)">
                <template #default="scope">
                    <span>{{ scope.row.Nodes.filter(c=>c.indexOf(state.prefix)<0).map(c=>state.nodes[c]).join(',') }}</span>
                </template>
            </el-table-column>
            <el-table-column v-if="state.prefix" prop="Nodes1" :label="$t(`server.wlistNodes${state.page.Type}`)">
                <template #default="scope">
                    <span>{{ scope.row.Nodes.filter(c=>c.indexOf(state.prefix)>=0).map(c=>c.replace(state.prefix,'')).join(',') }}</span>
                </template>
            </el-table-column>
            <el-table-column prop="Bandwidth" label="Mbps" width="80"></el-table-column>
            <el-table-column prop="Remark" :label="$t('server.wlistRemark')"></el-table-column>
            <el-table-column prop="UseTime" :label="`${$t('server.wlistUseTime')}`" width="140"></el-table-column>
            <el-table-column prop="EndTime" :label="`${$t('server.wlistEndTime')}`" width="140"></el-table-column>
            <el-table-column prop="AddTime" :label="`${$t('server.wlistAddTime')}`" width="140"></el-table-column>
            <el-table-column fixed="right" prop="Oper" :label="$t('server.wlistOper')" width="110">
                <template #default="scope">
                    <el-button size="small" @click="handleEdit(scope.row)">
                        <el-icon><EditPen /></el-icon>
                    </el-button>
                    <el-popconfirm :title="$t('server.wlistDelConfirm')" @confirm="handleDel(scope.row)">
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
import { wlistDel, wlistPage } from '@/apis/wlist';
export default {
    props: ['modelValue','type','prefix'],
    emits: ['update:modelValue'],
    components:{Delete,Plus,Search ,EditPen,Add,Warning},
    setup(props,{emit}) {
        const {t} = useI18n();
        const nodes = inject('nodes');
        const state = reactive({
            nodes:computed(()=>[{Id:'*',Name:'*'}].concat(nodes.value).reduce((json,item,index)=>{ json[item.Id] = item.Name; return json; },{})),
            page:{
                Page:1,
                Size:10,
                Type:props.type,
                MachineId:'',
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
            showAdd:false,
            prefix:props.prefix
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
            editState.value = {Id:0,Name:'',Nodes:['*'],Remark:'',UserId:'',Type:props.type,prefix:props.prefix || ''};
            state.showAdd = true;
        }
        const handleEdit = (row)=>{
            row.prefix = props.prefix || '';
            editState.value = row;
            state.showAdd = true;
        }
        const handleSearch = ()=>{
            wlistPage(state.page).then((res)=>{
                state.list = res;
            }).catch(()=>{})
        }
        const handlePageChange = (p)=>{
            state.page.Page = p;
            handleSearch();
        }
        const handleDel = (row)=>{
            wlistDel(row.Id).then((res)=>{
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