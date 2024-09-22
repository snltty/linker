<template>
    <div style="width: 30rem;padding: 5rem 0; margin:  0 auto;">
        <p class="t-c">
            服务器更新密钥
        </p>
        <p>
            <el-input type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange" />
        </p>
    </div>
</template>
<script>
import { getSecretKey,setSecretKey } from '@/apis/updater';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, inject, onMounted, reactive } from 'vue'
export default {
    label:'服务器更新',
    name:'updater',
    order:6,
    setup(props) {
        const globalData = injectGlobalData();
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