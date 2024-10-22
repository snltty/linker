<template>
    <el-form-item label="服务器更新密钥">
        <div class="flex">
            <el-input class="flex-1" type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange"/>
            <span>密钥正确时可更新服务端</span>
        </div>
    </el-form-item>
</template>
<script>
import { getSecretKey,setSecretKey } from '@/apis/updater';
import { ElMessage } from 'element-plus';
import { onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const state = reactive({
            secretKey:''
        });
        const _getSecretKey = ()=>{
            getSecretKey().then((res)=>{
                state.secretKey = res;
            });
        }

        const _setSecretKey = ()=>{
            if(!state.secretKey) return;
            setSecretKey(state.secretKey).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });
        }
        const handleChange = ()=>{
            _setSecretKey();
        }

        onMounted(()=>{
            _getSecretKey();
        });

        return {state,handleChange}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>