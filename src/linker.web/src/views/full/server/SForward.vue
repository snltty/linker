<template>
    <el-form-item :label="$t('server.sforwardSecretKey')">
        <div>
            <div class="flex">
                <el-input :class="{success:state.keyState,error:state.keyState==false}" style="width:20rem;" type="password" show-password v-model="state.SForwardSecretKey" maxlength="36" @blur="handleChange" />
                <Sync class="mgl-1" name="SForwardSecretKey"></Sync>
                <span class="mgl-1" v-if="globalData.isPc">{{$t('server.sforwardText')}}</span>
            </div>
            <div class="flex">
                <Cdkey type="SForward"></Cdkey>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { checkSForwardKey, getSForwardSecretKey,setSForwardSecretKey } from '@/apis/sforward';
import { ElMessage } from 'element-plus';
import {onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
import { injectGlobalData } from '@/provide';
import Cdkey from './cdkey/Index.vue'
export default {
    components:{Sync,Cdkey},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            SForwardSecretKey:'',
            keyState:false
        });

        const _getSForwardSecretKey = ()=>{
            getSForwardSecretKey().then((res)=>{
                state.SForwardSecretKey = res;
                handleCheckKey();
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
            handleCheckKey();
        }
        const handleCheckKey = ()=>{
            checkSForwardKey(state.SForwardSecretKey).then((res)=>{
                state.keyState = res;
            });
        }

        onMounted(()=>{
            _getSForwardSecretKey();
        });
        

        return {globalData,state,handleChange}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>