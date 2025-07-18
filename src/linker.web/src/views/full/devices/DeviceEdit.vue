<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" :title="`设置[${state.ruleForm.MachineName}]设备`" width="360">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="">
                    <div>修改后最好能重启一次客户端</div>
                </el-form-item>
                <el-form-item label="设备名" prop="MachineName">
                    <el-input v-trim maxlength="32" show-word-limit v-model="state.ruleForm.MachineName" />
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
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

export default {
    props: ['data','modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                MachineName1: props.data.MachineName,
                MachineName: props.data.MachineName,
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
            if(props.data.MachineName == state.ruleForm.MachineName) return;
            setSignInName({
                Id:props.data.MachineId,
                newName:state.ruleForm.MachineName
            }).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
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