<template>
   <el-dialog class="options-center" :title="$t('server.sforwardTitle')" destroy-on-close v-model="state.show" width="34rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.sforwardName')" prop="Name">
                    <el-input :disabled="data._manager==false" v-trim minlength="1" maxlength="32" show-word-limit v-model="state.ruleForm.Name" />
                </el-form-item>
                <el-form-item :label="$t('server.sforwardHost')" prop="Host">
                    <el-input v-trim v-model="state.ruleForm.Host" />
                </el-form-item>
                <el-form-item :label="$t('server.sforwardDomain')" prop="Domain">
                    <el-input :disabled="data._manager==false" v-trim v-model="state.ruleForm.Domain" />
                </el-form-item>
                <el-form-item :label="$t('server.sforwardConnection')" prop="Connections">
                    <el-input-number :disabled="data._manager==false" v-model="state.ruleForm.Connections" :min="0" :max="65535"/>
                </el-form-item>
                <el-form-item :label="$t('server.sforwardSpeed')" prop="BandwidthEach">
                    <el-input-number v-model="state.ruleForm.BandwidthEach" :min="0"/>Mbps
                </el-form-item>
                <el-form-item :label="$t('server.sforwardSpeed1')" prop="Bandwidth">
                    <el-input-number :disabled="data._manager==false" v-model="state.ruleForm.Bandwidth" :min="0"/>Mbps
                </el-form-item>
                <el-form-item :label="$t('server.sforwardFlow')" prop="DataEachMonth">
                    <el-input-number :disabled="data._manager==false" v-model="state.ruleForm.DataEachMonth" :min="0"/>GB <el-button size="small" @click="handleRefresh"><el-icon><Refresh /></el-icon></el-button>
                </el-form-item>
                <el-form-item :label="$t('server.sforwardFlowLast')" prop="DataRemain">
                    <el-input-number :disabled="data._manager==false" v-model="state.ruleForm.DataRemain" :min="0" />byte
                </el-form-item>
                <el-form-item :label="$t('server.sforwardUrl')" prop="Url">
                    <el-input :disabled="data._manager==false" v-trim v-model="state.ruleForm.Url" />
                </el-form-item>
                <el-form-item :label="$t('server.sforwardLogo')" prop="Logo">
                    <el-input :disabled="data._manager==false" v-trim v-model="state.ruleForm.Logo" />
                </el-form-item>
                <el-form-item :label="$t('server.sforwardPublic')" prop="Public">
                    <el-switch v-model="state.ruleForm.Public " size="small" />
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
import { Refresh } from '@element-plus/icons-vue'
import { sforwardUpdate } from '@/apis/sforward';
export default {
    props: ['data','modelValue'],
    emits: ['update:modelValue','success'],
    components:{Refresh},
    setup(props,{emit}) {
        const {t} = useI18n();
        const state = reactive({
            show:true,
            ruleForm:JSON.parse(JSON.stringify(props.data)),
            rules:{
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleRefresh = ()=>{
            state.ruleForm.DataRemain = state.ruleForm.DataEachMonth * 1024*1024*1024;
        }

        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));

                sforwardUpdate(json).then((res)=>{
                    if(res){
                        ElMessage.success(t('common.oper'));
                        state.show = false;
                        emit('success');
                    }else{
                        ElMessage.error(t('common.operFail'));
                    }
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }
        return {state,ruleFormRef,handleRefresh,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>