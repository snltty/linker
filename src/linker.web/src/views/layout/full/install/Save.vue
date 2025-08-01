<template>
    <div>
        <el-form ref="formDom" :model="state.ruleForm" :rules="state.rules" label-width="auto">
            <el-form-item label="服务器" prop="server">
                <el-input v-trim v-model="state.ruleForm.server" />
            </el-form-item>
            <el-form-item label="密钥" prop="value">
                <el-input v-trim v-model="state.ruleForm.value" />
            </el-form-item>
            <el-form-item label="" prop="Btns">
                <div class="t-c w-100">
                    <el-button type="primary" @click="handleSave">确认</el-button>
                </div>
            </el-form-item>
        </el-form>
    </div>
</template>

<script>
import { installSave } from '@/apis/config';
import { ElMessage } from 'element-plus';
import { reactive, ref } from 'vue';

export default {
    setup () {
        
        const state = reactive({ 
            ruleForm: {
                server: '',
                value:'',
            },
            rules: {
                server: [{ required: true, message: "必填", trigger: "blur" }],
                value: [{ required: true, message: "必填", trigger: "blur" }],
            }
        })
        const formDom = ref(null);
        const handleSave = ()=>{
            formDom.value.validate((valid) => {
                if (!valid) return;
                installSave(state.ruleForm).then((res)=>{
                    if(!res){
                        ElMessage.error('保存失败，可能服务器或者密钥不正确，或者密钥已被使用');
                        return;
                    }
                    ElMessage.success('保存成功');
                    window.location.reload();
                }).catch(()=>{
                    ElMessage.error('保存失败');
                })
            });
        }
        return {
            state,formDom,handleSave
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>