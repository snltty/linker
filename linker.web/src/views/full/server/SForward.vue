<template>
    <div style="width: 30rem;padding: 5rem 0; margin:  0 auto;">
        <p class="t-c">
            服务器代理穿透密钥
        </p>
        <p>
            <el-input type="password" show-password v-model="state.SForwardSecretKey" maxlength="36" @blur="handleChange" />
        </p>
    </div>
</template>
<script>
import { getSForwardSecretKey,setSForwardSecretKey } from '@/apis/sforward';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import {onMounted, reactive } from 'vue'
export default {
    label:'服务器穿透',
    name:'sforward',
    order:5,
    setup(props) {
        const globalData = injectGlobalData();
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
            }).catch(()=>{
                ElMessage.success('操作失败');
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