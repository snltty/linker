<template>
    <el-form-item label="服务器更新密钥">
        <div class="flex">
            <el-input class="flex-1" type="password" show-password v-model="state.secretKey" maxlength="36" @blur="handleChange"/>
            <span>密钥正确时可更新服务端</span>
        </div>
    </el-form-item>
    <el-form-item label="客户端更新检测频率">
        <div>
           <div>
                <el-input-number v-model="state.year" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> 年
                <el-input-number v-model="state.month" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> 月
                <el-input-number v-model="state.day" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> 日
           </div>
            <div>
                <el-input-number v-model="state.hour" :min="0" :max="99" style="width:12rem" @change="handleSecChange" /> 时
                <el-input-number v-model="state.min" :min="0" :max="99" style="width:12rem" @change="handleSecChange"/> 分
                <el-input-number v-model="state.sec" :min="0" :max="99" style="width:12rem" @change="handleSecChange"/> 秒
            </div>
        </div>
    </el-form-item>
</template>
<script>
import { getSecretKey,setSecretKey, setUpdateInterval } from '@/apis/updater';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { onMounted, reactive } from 'vue'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            secretKey:'',
            year:0,
            month:0,
            day:0,
            hour:0,
            min:1,
            sec:0,
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
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }
       const _setUpdateInterval = ()=>{
            const seconds = state.year*31536000 + state.month*2592000 + state.day*86400 + state.hour*3600 + state.min*60 + state.sec;
            setUpdateInterval(seconds).then(()=>{
                ElMessage.success('已操作');
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
       }
       const handleSecChange = ()=>{
        _setUpdateInterval();
       }

        const handleChange = ()=>{
            _setSecretKey();
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

        return {state,handleChange,handleSecChange}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>