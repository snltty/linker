<template>
    <el-form-item label="服务器穿透密钥">
        <div class="flex">
            <el-input class="flex-1" type="password" show-password v-model="state.SForwardSecretKey" maxlength="36" @blur="handleChange" />
            <span>密钥正确时可使用内网穿透</span>
        </div>
    </el-form-item>
</template>
<script>
import { getSForwardSecretKey,setSForwardSecretKey } from '@/apis/sforward';
import { ElMessage } from 'element-plus';
import {onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const state = reactive({
            SForwardSecretKey:''
        });

        const _getSForwardSecretKey = ()=>{
            getSForwardSecretKey().then((res)=>{
                state.SForwardSecretKey = res;
            });
        }

        const _setSForwardSecretKey = ()=>{
            if(!state.SForwardSecretKey) return;
            setSForwardSecretKey(state.SForwardSecretKey).then(()=>{
                ElMessage.success('已操作');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }
        const handleChange = ()=>{
            _setSForwardSecretKey();
        }

        onMounted(()=>{
            _getSForwardSecretKey();
        });

        return {state,handleChange}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>