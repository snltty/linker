<template>
   <el-dialog class="options-center" :title="$t('server.cdkeyTestTitle')" destroy-on-close v-model="state.show" width="40rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.cdkeyTestKey')" prop="Base64">
                    <el-input v-trim v-model="state.Base64" @change="handleChange" />
                </el-form-item>
                <el-form-item label="" v-if="state.ruleForm.Field.indexOf('Parse')>=0">
                    <div class="t-c w-100">
                        <span class="red">{{$t('server.cdkeyTestParseError')}}</span>
                    </div>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestGB')" prop="GB">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.GB" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('GB')>=0">{{$t('server.cdkeyTestGBError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestSpeed')" prop="Speed">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Speed" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Speed')>=0">{{$t('server.cdkeyTestSpeedError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestTime')" prop="Time">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Time" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Time')>=0">{{$t('server.cdkeyTestTimeError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestUserId')" prop="WidgetUserId">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.WidgetUserId" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('UserId')>=0">{{$t('server.cdkeyTestUserIdError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestOrderId')" prop="OrderId">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.OrderId" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('OrderId')>=0">{{$t('server.cdkeyTestOrderIdError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestContact')" prop="Contact">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Contact" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Contact')>=0">{{$t('server.cdkeyTestContactError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestCostPrice')" prop="CostPrice">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.CostPrice" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('CostPrice')>=0">{{$t('server.cdkeyTestCostPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestPrice')" prop="Price">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Price" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Price')>=0">{{$t('server.cdkeyTestPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestUserPrice')" prop="UserPrice">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.UserPrice" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('UserPrice')>=0">{{$t('server.cdkeyTestUserPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestPayPrice')" prop="PayPrice">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.PayPrice" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('PayPrice')>=0">{{$t('server.cdkeyTestPayPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestCount')" prop="Count">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Count" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Count')>=0">{{$t('server.cdkeyTestCountError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.cdkeyTestType')" prop="Type">
                    <el-row class="w-100">
                        <el-col :span="15">
                            <el-input v-trim v-model="state.ruleForm.Order.Type" />
                        </el-col>
                        <el-col :span="9">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Type')>=0 || state.ruleForm.Order.Type !='Relay' ">{{$t('server.cdkeyTestTypeError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleChange">{{$t('common.confirm')}}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>

<script>
import { reactive, watch } from 'vue'
import { Search } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
import { cdkeyTest } from '@/apis/cdkey';
export default {
    props: ['modelValue','type'],
    emits: ['update:modelValue'],
    components:{Search},
    setup(props,{emit}) {
        const {t} = useI18n();
        const state = reactive({
            show:true,
            Base64:'',
            ruleForm:{
                Order:{},
                Cdkey:'',
                Field:[],
                Type:props.type
            },
            rules:{}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleChange = ()=>{
            cdkeyTest({Base64:state.Base64}).then(res=>{
                if(res.Cdkey) state.ruleForm.Cdkey = res.Cdkey;
                if(res.Field)  state.ruleForm.Field = res.Field;
                if(res.Order) state.ruleForm.Order = res.Order;
            }).catch(()=>{})
        }

        return {state,handleChange}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>