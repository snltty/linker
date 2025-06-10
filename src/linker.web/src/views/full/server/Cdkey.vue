<template>
    <el-form-item :label="$t('server.cdkeySecretKey')">
        <div class="flex">
            <el-input :class="{success:state.keyState,error:state.keyState==false}" class="flex-1" type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange"/>
            <Sync class="mgl-1" name="CdkeySecretKey"></Sync>
            <span class="mgl-1" v-if="globalData.isPc">{{$t('server.cdkeyText')}}</span>
        </div>
    </el-form-item>
</template>
<script>
import { cdkeyAccess, getSecretKey,setSecretKey } from '@/apis/cdkey';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
export default {
    components:{Sync},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            secretKey:'',
            keyState:false,
        });
        const _getSecretKey = ()=>{
            getSecretKey().then((res)=>{
                state.secretKey = res;
                handleCheckKey();
            });
        }

        const _setSecretKey = ()=>{
            if(!state.secretKey) return;
            setSecretKey(state.secretKey).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleChange = ()=>{
            _setSecretKey();
            handleCheckKey();
        }
        const handleCheckKey = ()=>{
            cdkeyAccess().then((res)=>{
                state.keyState = res;
            }).catch(()=>{});
        }

        onMounted(()=>{
            _getSecretKey();
        });

        return {globalData,state,handleChange}
    }
}
</script>
<style lang="stylus" scoped>

</style>