<template>
    <el-dialog class="options-center" :title="$t('server.relayMyCdkey')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
        <div class="head">
            <div class="search flex">
                <div><span>{{$t('server.relayCdkeyOrderId')}}</span> <el-input v-model="state.page.OrderId" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
                <div><span>{{$t('server.relayCdkeyContact')}}</span> <el-input v-model="state.page.Contact" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
                <div><span>{{$t('server.relayCdkeyRemark')}}</span> <el-input v-model="state.page.Remark" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
                <div>
                    <el-button size="small" @click="handleSearch()">
                        <el-icon><Search /></el-icon>
                    </el-button>
                </div>
                <div>
                    <el-button size="small" type="success" @click="handleImport">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                </div>
            </div>
            <Flags @change="handleFlagsChange"></Flags>
        </div>
        <el-table stripe  :data="state.list.List" border size="small" width="100%" @sort-change="handleSort">
            <el-table-column prop="Bandwidth" :label="$t('server.relayCdkeyBandwidth')" width="110" sortable="custom">
                <template #default="scope">{{ scope.row.Bandwidth }}Mbps</template>
            </el-table-column>
            <el-table-column prop="LastBytes" :label="`${$t('server.relayCdkeyBytes')}`" width="80" sortable="custom">
                <template #default="scope">
                    <p><strong>{{ parseSpeed(scope.row.LastBytes) }}</strong></p>
                    <p>{{ parseSpeed(scope.row.MaxBytes) }}</p>
                </template>
            </el-table-column>
            <el-table-column prop="PayPrice" :label="`${$t('server.relayCdkeyPay')}`" width="120" sortable="custom">
                <template #default="scope">
                    <p><strong>{{$t('server.relayCdkeyPayPrice')}}.{{ scope.row.PayPrice }}</strong>/{{$t('server.relayCdkeyPrice')}}.{{ scope.row.Price }}</p>
                    <p>{{$t('server.relayCdkeyUserPrice')}}.{{ scope.row.UserPrice }}/{{$t('server.relayCdkeyCostPrice')}}.{{ scope.row.CostPrice }}</p>
                </template>
            </el-table-column>
            <el-table-column prop="OrderId" :label="`${$t('server.relayCdkeyOrder')}`" width="180">
                <template #default="scope">
                    <p>{{ scope.row.OrderId }}</p>
                    <p>{{ scope.row.Contact }}</p>
                </template>
            </el-table-column>
            <el-table-column prop="Remark" :label="$t('server.relayCdkeyRemark')">
            </el-table-column>
            <el-table-column prop="EndTime" :label="`${$t('server.relayCdkeyEndTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column prop="UseTime" :label="`${$t('server.relayCdkeyUseTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column prop="AddTime" :label="`${$t('server.relayCdkeyAddTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column fixed="right" prop="Oper" :label="$t('server.relayCdkeyOper')" width="60">
                <template #default="scope">
                    <div v-if="scope.row.Deleted == false">
                        <el-popconfirm :title="$t('server.relayCdkeyDelConfirm')" @confirm="handleDel(scope.row)">
                            <template #reference>
                                <el-button type="danger" size="small">
                                    <el-icon><Delete /></el-icon>
                                </el-button>
                            </template>
                        </el-popconfirm>
                    </div>
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
   
</template>

<script>
import { injectGlobalData } from '@/provide';
import { onMounted, reactive,  watch } from 'vue'
import { Delete,Plus,Search } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import {relayCdkeyMy,relayCdkeyDel, relayCdkeyImport } from '@/apis/relay';
import Flags from './Flags.vue';
import { ElMessage, ElMessageBox } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Delete,Plus,Search,Flags },
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            page:{
                Page:1,
                Size:10,
                Order:'',
                Sort:'',
                OrderId:'',
                Contact:'',
                Remark:'',
                Flag:0
            },
            list:{
                Page:1,
                Size:15,
                Count:0,
                List:[]
            },
            show:true
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const parseSpeed = (num) => {
            let index = 0;
            while (num >= 1024) {
                num /= 1024;
                index++;
            }
            return `${(num*1.0).toFixed(2)}${['B', 'KB', 'MB', 'GB', 'TB'][index]}`;
        }

        const handleFlagsChange = (flag)=>{
            state.page.Flag = flag;
            handleSearch();
        }
        const handleSearch = ()=>{
            relayCdkeyMy(state.page).then((res)=>{
                state.list = res;
            }).catch(()=>{})
        }
        const handlePageChange = (p)=>{
            state.page.Page = p;
            handleSearch();
        }
        const handleSort = (a)=>{
            state.page.Order = a.prop;
            state.page.Sort = {'ascending':'asc','descending':'desc'}[a.order];
            handleSearch();
        }
        const handleDel = (row)=>{
            relayCdkeyDel(row.Id).then((res)=>{
                handleSearch();
            }).catch(()=>{})
        }

        const handleImport = ()=>{
            ElMessageBox.prompt(t('server.relayCdkeyImport'), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel')
            }).then(({ value }) => {
                if(!value){
                    handleImport();
                    return;
                }

                relayCdkeyImport({Base64:value}).then((res)=>{
                    if(res){
                        ElMessage.error(t(`server.relayCdkeyImport${res}`));
                        handleImport();
                    }else{
                        ElMessage.success(t('common.oper'));
                        handleSearch();
                    }
                }).catch(()=>{})
            }).catch(() => {
            })
        }
        onMounted(()=>{
            handleSearch();
        })

        return {state,parseSpeed,handleSort,handleFlagsChange,handleSearch,handlePageChange,handleDel,handleImport}
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