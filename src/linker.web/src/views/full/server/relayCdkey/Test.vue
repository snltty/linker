<template>
   <el-dialog class="options-center" :title="$t('server.relayCdkeyTestTitle')" destroy-on-close v-model="state.show" width="40rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.relayCdkeyTestKey')" prop="Base64">
                    <el-input v-model="state.Base64" @change="handleChange" />
                </el-form-item>
                <el-form-item label="" v-if="state.ruleForm.Field.indexOf('Parse')>=0">
                    <div class="t-c w-100">
                        <span class="red">{{$t('server.relayCdkeyTestParseError')}}</span>
                    </div>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestGB')" prop="GB">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.GB" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('GB')>=0">{{$t('server.relayCdkeyTestGBError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestSpeed')" prop="Speed">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.Speed" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Speed')>=0">{{$t('server.relayCdkeyTestSpeedError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestTime')" prop="Time">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.Time" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Time')>=0">{{$t('server.relayCdkeyTestTimeError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestUserId')" prop="WidgetUserId">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.WidgetUserId" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('UserId')>=0">{{$t('server.relayCdkeyTestUserIdError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestOrderId')" prop="OrderId">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.OrderId" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('OrderId')>=0">{{$t('server.relayCdkeyTestOrderIdError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestContact')" prop="Contact">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.Contact" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Contact')>=0">{{$t('server.relayCdkeyTestContactError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestCostPrice')" prop="CostPrice">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.CostPrice" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('CostPrice')>=0">{{$t('server.relayCdkeyTestCostPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestPrice')" prop="Price">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.Price" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Price')>=0">{{$t('server.relayCdkeyTestPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestUserPrice')" prop="UserPrice">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.UserPrice" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('UserPrice')>=0">{{$t('server.relayCdkeyTestUserPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestPayPrice')" prop="PayPrice">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.PayPrice" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('PayPrice')>=0">{{$t('server.relayCdkeyTestPayPriceError')}}</span>
                            <span v-else class="green">success</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.relayCdkeyTestCount')" prop="Count">
                    <el-row>
                        <el-col :span="18">
                            <el-input v-model="state.ruleForm.Order.Count" />
                        </el-col>
                        <el-col :span="6">
                            <span class="red" v-if="state.ruleForm.Field.indexOf('Count')>=0">{{$t('server.relayCdkeyTestCountError')}}</span>
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
import { relayCdkeyTest } from '@/apis/relay';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Search},
    setup(props,{emit}) {
        const {t} = useI18n();
        const state = reactive({
            show:true,
            Base64:'qn1vYOBtu81DkBI10nTTw7h/Vzi1Do3X1HI2+P5fflINL2wNPxrUYQPeOxUqgT+RcZSKLGF3dCMOyYBB+1VKmuI6ZaN86Whr1Xux2dY25tI5/3i/x/K1S78E6+E70ruh61phQZT3QLLVAHnIa2HpFhtPLKZaWc++ReSzixBqkW/sTT/l2iDqG2rl/zKynRM6srXWJkJr+2Msme1u5/Qu/0o6VaJ3ylv71HlB4zubNybQX5WMsOWrjN4/ruRSVF0LcayshqEfwlpeGR44nn5jMxpgQhxxpk/1flVImF00cC4=',
            ruleForm:{
                Order:{},
                Cdkey:'',
                Field:[],
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
            relayCdkeyTest({Base64:state.Base64}).then(res=>{
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