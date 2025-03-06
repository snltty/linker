<template>
    <el-dialog class="options-center" :title="$t('server.relayCdkey')" destroy-on-close v-model="state.show" width="77rem" top="2vh">
    <div class="group-wrap">
        <div class="head flex">
            <div><span>用户id</span> <el-input v-model="state.page.UserId" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
            <div><span>备注</span> <el-input v-model="state.page.Remark" style="width:10rem" size="small" clearable @change="handleSearch" /></div>
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
        <el-table stripe  :data="state.list.List" border size="small" width="100%" @cell-dblclick="handleCellClick">
            <el-table-column prop="Bandwidth" :label="$t('server.relayCdkeyBandwidth')" width="110" sortable="custom">
                <template #default="scope">{{ scope.row.Bandwidth }}Mbps</template>
            </el-table-column>
            <el-table-column prop="LastBytes" :label="`${$t('server.relayCdkeyLastBytes')}/${$t('server.relayCdkeyMaxBytes')}`" width="160" sortable="custom">
                <template #default="scope">{{ parseSpeed(scope.row.LastBytes) }}/{{ parseSpeed(scope.row.MaxBytes) }}</template>
            </el-table-column>
            <el-table-column prop="PayMemory" :label="`${$t('server.relayCdkeyPayMemory')}/${$t('server.relayCdkeyMemory')}`" width="120" sortable="custom">
                <template #default="scope">{{ scope.row.PayMemory }}/{{ scope.row.Memory }}</template>
            </el-table-column>
            <el-table-column prop="Remark" :label="$t('server.relayCdkeyRemark')">
            </el-table-column>
            <el-table-column prop="EndTime" :label="`${$t('server.relayCdkeyEndTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column prop="UseTime" :label="`${$t('server.relayCdkeyUseTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column prop="StartTime" :label="`${$t('server.relayCdkeyStartTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column prop="AddTime" :label="`${$t('server.relayCdkeyAddTime')}`" width="140" sortable="custom">
            </el-table-column>
            <el-table-column fixed="right" prop="Oper" :label="$t('server.relayCdkeyOper')" width="60">
                <template #default="scope">
                    <div>
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
    <el-dialog class="options-center" :title="$t('server.relayCdkey')" destroy-on-close v-model="state.showAdd" width="42rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.relayCdkeyUserId')" prop="UserId">
                    <el-input maxlength="32" show-word-limit v-model="state.ruleForm.UserId" />
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyBandwidth')" prop="Bandwidth">
                    <el-input-number size="small" v-model="state.ruleForm.Bandwidth" :min="1" :max="102400" />Mbps
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyMaxBytes')" prop="MaxBytes">
                    <el-input-number size="small" v-model="state.ruleForm.G" :min="0" :max="102400" />GB
                    <el-input-number size="small" v-model="state.ruleForm.M" :min="0" :max="1024" />MB
                    <el-input-number size="small" v-model="state.ruleForm.K" :min="0" :max="1024" />KB
                    <el-input-number size="small" v-model="state.ruleForm.B" :min="0" :max="1024" />B
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.relayCdkeyStartTime')" prop="StartTime">
                    <el-date-picker v-model="state.ruleForm.StartTime" type="datetime" placeholder="Select date and time"/>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyEndTime')" prop="EndTime">
                    <el-date-picker v-model="state.ruleForm.EndTime" type="datetime" placeholder="Select date and time"/>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.relayCdkeyMemory')" prop="Memory">
                    <el-input-number size="small" v-model="state.ruleForm.Memory" :min="0" />
                    {{ $t('server.relayCdkeyPayMemory') }}
                    <el-input-number size="small" v-model="state.ruleForm.PayMemory" :min="0" />
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyRemark')" prop="Remark">
                    <el-input v-model="state.ruleForm.Remark" />
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
                StartTime:new Date(),
                EndTime:new Date(new Date().getFullYear() + 1,new Date().getMonth(),new Date().getDay(),new Date().getHours(),new Date().getMinutes(),new Date().getSeconds()),
                Memory:0,
                PayMemory:0,
                Remark:'',
            };
        const state = reactive({
            page:{
                Page:1,
                Size:15,
                Order:'',
                Sort:'',
                Userid:'',
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
                json.StartTime = moment(json.StartTime).format("YYYY-MM-DD HH:mm:ss");
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

        return {state,ruleFormRef,parseSpeed,handleSearch,handlePageChange,handleAdd,handleDel,handleSave}
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