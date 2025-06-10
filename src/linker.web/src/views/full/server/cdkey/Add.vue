<template>
   <el-dialog class="options-center" :title="$t('server.addCdkey')" destroy-on-close v-model="state.show" width="60rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.cdkeyUserId')" prop="UserId">
                    <el-input maxlength="36" show-word-limit v-model="state.ruleForm.UserId" />
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyBandwidth')" prop="Bandwidth">
                    <el-input-number size="small" v-model="state.ruleForm.Bandwidth" :min="1" :max="102400" />Mbps
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyBytes')" prop="MaxBytes">
                    <el-input-number size="small" v-model="state.ruleForm.G" :min="0" :max="102400" />GB
                    <el-input-number size="small" v-model="state.ruleForm.M" :min="0" :max="1024" />MB
                    <el-input-number size="small" v-model="state.ruleForm.K" :min="0" :max="1024" />KB
                    <el-input-number size="small" v-model="state.ruleForm.B" :min="0" :max="1024" />B
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.cdkeyDuration')" prop="EndTime">
                    <p>
                        <el-input-number size="small" v-model="state.ruleForm.Year" :min="0" />{{$t('server.cdkeyYear')}}
                        <el-input-number size="small" v-model="state.ruleForm.Month" :min="0" />{{$t('server.cdkeyMonth')}}
                        <el-input-number size="small" v-model="state.ruleForm.Day" :min="0" />{{$t('server.cdkeyDay')}}
                    </p>
                    <p>
                        <el-input-number size="small" v-model="state.ruleForm.Hour" :min="0" />{{$t('server.cdkeyHour')}}
                        <el-input-number size="small" v-model="state.ruleForm.Min" :min="0" />{{$t('server.cdkeyMin')}}
                        <el-input-number size="small" v-model="state.ruleForm.Sec" :min="0" />{{$t('server.cdkeySec')}}
                    </p>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('server.cdkeyCostPrice')" prop="CostPrice">
                    <el-input-number size="small" v-model="state.ruleForm.CostPrice" :min="0" />
                    {{ $t('server.cdkeyPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.Price" :min="0" />
                    {{ $t('server.cdkeyUserPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.UserPrice" :min="0" />
                    {{ $t('server.cdkeyPayPrice') }}
                    <el-input-number size="small" v-model="state.ruleForm.PayPrice" :min="0" />
                </el-form-item>
                <el-form-item label="">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item :label="$t('server.cdkeyRemark')" prop="Remark">
                                <el-input v-model="state.ruleForm.Remark" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item :label="$t('server.cdkeyContact')" prop="Contact">
                                <el-input v-model="state.ruleForm.Contact" />
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>

<script>
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import moment from "moment";
import { cdkeyAdd } from '@/apis/cdkey';
export default {
    props: ['modelValue','type'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();
        const state = reactive({
            show:true,
            ruleForm:{
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
                Type:props.type,
            },
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
        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));

                const date = new Date();
                const end = new Date(date.getFullYear()+json.Year,date.getMonth()+json.Month,date.getDate()+json.Day,date.getHours()+json.Hour,date.getMinutes()+json.Min,date.getSeconds()+json.Sec);
                json.EndTime = moment(end).format("YYYY-MM-DD HH:mm:ss");
                json.MaxBytes = json.G*1024*1024*1024 + json.M*1024*1024 + json.K*1024 + json.B;
                cdkeyAdd(json).then(()=>{
                    ElMessage.success(t('common.oper'));
                    state.show = false;
                    emit('success');
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }
        return {state,ruleFormRef,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>