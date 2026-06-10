<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" :title="$t('device.title',[state.ruleForm.MachineName])" width="360">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="">
                    <div>{{$t('device.restart')}}</div>
                </el-form-item>
                <el-form-item :label="$t('device.name')" prop="MachineName">
                    <el-input v-trim maxlength="32" show-word-limit v-model="state.ruleForm.MachineName" />
                </el-form-item>
                <el-form-item :label="$t('device.avatar')" prop="Avatar">
                    <el-input v-trim v-model="state.ruleForm.Avatar" />
                </el-form-item>
                <el-form-item label="--" prop="A">
                    <div>
                        <p>url : https://xx.xx.com/xx.jpg</p>
                        <p class="break-all">json : {"ff":"serif","fs":14,"fc":"#000",ft:"家",bc:"#f5f5f5"}</p>
                    </div>
                </el-form-item>
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
import { setSignInName } from '@/apis/signin';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';

export default {
    props: ['data','modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

        const {t} = useI18n();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                MachineName: props.data.MachineName,
                Avatar: props.data.Args['avatar'] || '',
            },
            rules: {}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleSave = () => {
            setSignInName({
                Id:props.data.MachineId,
                name:state.ruleForm.MachineName,
                avatar:state.ruleForm.Avatar,
            }).then(() => {
                state.show = false;
                ElMessage.success(t('common.opered'));
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        return {
           state, ruleFormRef,  handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>