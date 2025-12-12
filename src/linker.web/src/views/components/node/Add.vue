<template>
   <el-dialog class="options-center" :title="$t('server.denyAdd')" destroy-on-close v-model="state.show" width="28rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.denyStr')" prop="Str">
                    <el-input v-trim v-model="state.ruleForm.Str" />
                </el-form-item>
                <el-form-item :label="$t('server.denyRemark')" prop="Remark">
                    <el-input v-trim v-model="state.ruleForm.Remark" />
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
import { relayDenysAdd } from '@/apis/relay';
import { sforwardDenysAdd } from '@/apis/sforward';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue','type','data'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        console.log(props.data);
        const {t} = useI18n();
        const saveFn = props.type = 'relay' ? relayDenysAdd:sforwardDenysAdd
        const state = reactive({
            show:true,
            ruleForm:{
                NodeId:props.data.NodeId || '',
                Id:props.data.Id || 0,
                Str:props.data.Str || '',
                Remark:props.data.Remark || ''
            },    
            rules:{
                Str: [
                    { 
                        type:'string',required: true, message: "required", trigger: "blur",
                        pattern: /^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}(\/([0-9]|([1-2][0-9])|(3[0-2])))?)$|^0$|^\*$/,
                    }
                ],
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
                saveFn(json).then(()=>{
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