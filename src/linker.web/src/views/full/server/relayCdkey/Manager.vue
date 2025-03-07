<template>
    <el-dialog class="options-center" :title="$t('server.relayCdkey')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
        <div class="head flex">
            <div><span>{{$t('server.relayCdkeyUserId')}}</span> <el-input v-model="state.page.UserId" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
            <div><span>{{$t('server.relayCdkeyOrderId')}}</span> <el-input v-model="state.page.OrderId" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
            <div><span>{{$t('server.relayCdkeyContact')}}</span> <el-input v-model="state.page.Contact" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
            <div><span>{{$t('server.relayCdkeyRemark')}}</span> <el-input v-model="state.page.Remark" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
            <div>
                <el-button size="small" @click="handleSearch()">
                    <el-icon><Search /></el-icon>
                </el-button>
            </div>
            <div>
                <el-button size="small" type="success" @click="handleAdd()">
                    <el-icon><Plus /></el-icon>
                </el-button>
            </div>
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
    <el-dialog class="options-center" :title="$t('server.relayAddCdkey')" destroy-on-close v-model="state.showAdd" width="60rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.relayCdkeyUserId')" prop="UserId">
                    <el-input maxlength="32" show-word-limit v-model="state.ruleForm.UserId" />
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyBandwidth')" prop="Bandwidth">
                    <el-input-number size="small" v-model="state.ruleForm.Bandwidth" :min="1" :max="102400" />Mbps
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyBytes')" prop="MaxBytes">
                    <el-input-number size="small" v-model="state.ruleForm.G" :min="0" :max="102400" />GB
                    <el-input-number size="small" v-model="state.ruleForm.M" :min="0" :max="1024" />MB
                    <el-input-number size="small" v-model="state.ruleForm.K" :min="0" :max="1024" />KB
                    <el-input-number size="small" v-model="state.ruleForm.B" :min="0" :max="1024" />B
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.relayCdkeyDuration')" prop="EndTime">
                    <p>
                        <el-input-number size="small" v-model="state.ruleForm.Year" :min="0" />{{$t('server.relayCdkeyYear')}}
                        <el-input-number size="small" v-model="state.ruleForm.Month" :min="0" />{{$t('server.relayCdkeyMonth')}}
                        <el-input-number size="small" v-model="state.ruleForm.Day" :min="0" />{{$t('server.relayCdkeyDay')}}
                    </p>
                    <p>
                        <el-input-number size="small" v-model="state.ruleForm.Hour" :min="0" />{{$t('server.relayCdkeyHour')}}
                        <el-input-number size="small" v-model="state.ruleForm.Min" :min="0" />{{$t('server.relayCdkeyMin')}}
                        <el-input-number size="small" v-model="state.ruleForm.Sec" :min="0" />{{$t('server.relayCdkeySec')}}
                    </p>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.relayCdkeyCostPrice')" prop="CostPrice">
                    <el-input-number size="small" v-model="state.ruleForm.CostPrice" :min="0" />
                    {{ $t('server.relayCdkeyPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.Price" :min="0" />
                    {{ $t('server.relayCdkeyUserPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.UserPrice" :min="0" />
                    {{ $t('server.relayCdkeyPayPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.PayPrice" :min="0" />
                </el-form-item>
                <el-form-item label="">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item :label="$t('server.relayCdkeyRemark')" prop="Remark">
                                <el-input v-model="state.ruleForm.Remark" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item :label="$t('server.relayCdkeyContact')" prop="Contact">
                                <el-input v-model="state.ruleForm.Contact" />
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.showAdd = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, reactive, ref, watch } from 'vue'
import { Delete,Plus,Search } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import moment from "moment";
import { relayCdkeyAdd,relayCdkeyDel,relayCdkeyPage } from '@/apis/relay';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Delete,Plus,Search },
    setup(props,{emit}) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const defaultJson = {
                UserId:'',
                Bandwidth:1,
                G:1,
                M:0,
                K:0,
                B:0,
                Year:1,
                Month:0,
                Day:0,
                Hour:0,
                Min:0,
                Sec:0,
                CostPrice:0,
                Price:0,
                UserPrice:0,
                PayPrice:0,
                Remark:'hand',
                Contact:'',
            };
        const state = reactive({
            page:{
                Page:1,
                Size:10,
                Order:'',
                Sort:'',
                UserId:'',
                OrderId:'',
                Contact:'',
                Remark:'',
            },
            list:{
                Page:1,
                Size:15,
                Count:0,
                List:[]
            },
            show:true,
            showAdd:false,
            ruleForm:JSON.parse(JSON.stringify(defaultJson)),
            rules:{
                UserId: [{ required: true, message: "required", trigger: "blur" }],
                Remark: [{ required: true, message: "required", trigger: "blur" }],
            }
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

        const handleSearch = ()=>{
            relayCdkeyPage(state.page).then((res)=>{
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

        const handleAdd = (row)=>{
            state.ruleForm = JSON.parse(JSON.stringify(defaultJson));
            state.showAdd = true;
        }
        const handleDel = (row)=>{
            relayCdkeyDel(row.CdkeyId).then((res)=>{
                handleSearch();
            }).catch(()=>{})
        }
        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));

                const date = new Date();
                const end = new Date(date.getFullYear()+json.Year,date.getMonth()+json.Month,date.getDate()+json.Day,date.getHours()+json.Hour,date.getMinutes()+json.Min,date.getSeconds()+json.Sec);
                json.EndTime = moment(end).format("YYYY-MM-DD HH:mm:ss");
                json.MaxBytes = json.G*1024*1024*1024 + json.M*1024*1024 + json.K*1024 + json.B;

                relayCdkeyAdd(json).then(()=>{
                    ElMessage.success(t('common.oper'));
                    state.showAdd = false;
                    handleSearch();
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }


        onMounted(()=>{
            handleSearch();
        })

        return {state,ruleFormRef,parseSpeed,handleSort,handleSearch,handlePageChange,handleAdd,handleDel,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.head{
    &>div{
        margin-right:1rem;
    }
}
.page{
    padding:2rem 0;
    display:inline-block;
}
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>