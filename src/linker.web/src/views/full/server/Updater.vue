<template>
    <el-form-item :label="$t('server.updaterSecretKey')">
        <div >
            <div class="flex">
                <el-input :class="{success:state.keyState,error:state.keyState==false}" class="flex-1" type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange"/>
                <Sync class="mgl-1" name="UpdaterSecretKey"></Sync>
                <span class="mgl-1" v-if="globalData.isPc">{{$t('server.updaterText')}}</span>
            </div>
            <div>
                <el-checkbox v-model="state.sync2Server" @change="handleSync2ServerChange">{{ $t('server.updaterSync2Server') }}</el-checkbox>
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { checkUpdaterKey, getSecretKey,setSecretKey, setSync2Server } from '@/apis/updater';
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
            sync2Server:false,
            keyState:false,
        });
        const _getSecretKey = ()=>{
            getSecretKey().then((res)=>{
                state.secretKey = res.SecretKey;
                state.sync2Server = res.Sync2Server;
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
        const handleSync2ServerChange = ()=>{
            setSync2Server(state.sync2Server).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        const handleCheckKey = ()=>{
            checkUpdaterKey(state.secretKey).then((res)=>{
                state.keyState = res;
            }).catch(()=>{});
        }

        onMounted(()=>{
            _getSecretKey();
        });

        return {globalData,state,handleChange,handleSync2ServerChange}
    }
}
</script>
<style lang="stylus" scoped>

</style>