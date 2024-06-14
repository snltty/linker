<template>
    <div class="flex">
        <div class="pdr-10 pdb-6 flex-1">
            <el-checkbox v-model="state.sync" label="将更改同步到所有客户端"  />
        </div>
        <div>配置你的服务器代理穿透秘钥</div>
    </div>
    <div style="width: 30rem;padding: 5rem 0; margin:  0 auto;">
        <el-input type="password" show-password v-model="state.SForwardSecretKey" maxlength="36" @blur="handleChange" />
    </div>
</template>
<script>
import { getSForwardSecretKey,setSForwardSecretKey } from '@/apis/sforward';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            sync:true,
            SForwardSecretKey:''
        });

        const _getSForwardSecretKey = ()=>{
            getSForwardSecretKey().then((res)=>{
                state.SForwardSecretKey = res;
            });
        }

        const _setSForwardSecretKey = ()=>{
            if(!state.SForwardSecretKey) return;
            setSForwardSecretKey({
                sync:state.sync,
                SForwardSecretKey:state.SForwardSecretKey
            }).then(()=>{
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