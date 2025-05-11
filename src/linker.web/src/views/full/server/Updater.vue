<template>
    <el-form-item :label="$t('server.updaterSecretKey')">
        <div class="flex">
            <el-input :class="{success:state.keyState,error:state.keyState==false}" class="flex-1" type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange"/>
            <Sync class="mgl-1" name="UpdaterSecretKey"></Sync>
            <span class="mgl-1" v-if="globalData.isPc">{{$t('server.updaterText')}}</span>
        </div>
    </el-form-item>
    <!-- <el-form-item :label="$t('server.updaterRate')">
        <div>
           <div>
                <el-input-number v-model="state.year" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> {{$t('server.updaterY')  }}
                <el-input-number v-model="state.month" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> {{$t('server.updaterM')  }}
                <el-input-number v-model="state.day" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> {{$t('server.updaterD')  }}
           </div>
            <div>
                <el-input-number v-model="state.hour" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> {{$t('server.updaterH')  }}
                <el-input-number v-model="state.min" :min="0" :max="99" style="width:12rem" @change="handleSecChange"/> {{$t('server.updaterMM')  }}
                <el-input-number v-model="state.sec" :min="0" :max="99" style="width:12rem" @change="handleSecChange"/> {{$t('server.updaterS')  }}
            </div>
        </div>
    </el-form-item> -->
</template>
<script>
import { checkUpdaterKey, getSecretKey,setSecretKey, setUpdateInterval } from '@/apis/updater';
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
            year:0,
            month:0,
            day:0,
            hour:0,
            min:1,
            sec:0,
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
       const _setUpdateInterval = ()=>{
            const seconds = state.year*31536000 + state.month*2592000 + state.day*86400 + state.hour*3600 + state.min*60 + state.sec;
            setUpdateInterval(seconds).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
       }
       const handleSecChange = ()=>{
        _setUpdateInterval();
       }

        const handleChange = ()=>{
            _setSecretKey();
            handleCheckKey();
        }
        const handleCheckKey = ()=>{
            checkUpdaterKey(state.secretKey).then((res)=>{
                state.keyState = res;
            }).catch(()=>{});
        }

        onMounted(()=>{
            _getSecretKey();

            let seconds = globalData.value.config.Common.UpdateIntervalSeconds;
            state. year = Math.floor(seconds / 31536000);
            seconds %= 31536000;
            
            state. month = Math.floor(seconds / 2592000 );
            seconds %= 2592000 ;
            
            state.day = Math.floor(seconds / 86400) ;
            seconds %= 86400;
            
            state.hour =Math.floor( seconds / 3600);
            seconds %= 3600;
            
            state.min = Math.floor(seconds / 60);
            seconds %= 60 ;

            state.sec = seconds;
        });

        return {globalData,state,handleChange,handleSecChange}
    }
}
</script>
<style lang="stylus" scoped>

</style>