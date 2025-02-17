<template>
    <el-form-item :label="$t('server.sforwardSecretKey')">
        <div class="flex">
            <el-input class="flex-1" type="password" show-password v-model="state.SForwardSecretKey" maxlength="36" @blur="handleChange" />
            <span>{{$t('server.sforwardText')}}</span>
        </div>
    </el-form-item>
</template>
<script>
import { getSForwardSecretKey,setSForwardSecretKey } from '@/apis/sforward';
import { ElMessage } from 'element-plus';
import {onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n';
export default {
    setup(props) {
        const {t} = useI18n();
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
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
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