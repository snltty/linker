<template>
    <div v-if="config" class="status-api-wrap" :class="{connected:state.connected}">
        <el-popconfirm 
        :confirm-button-text="$t('api.clear')" 
        :cancel-button-text="$t('api.alter')" 
        :title="$t('common.confirmSure')" @cancel="handleShow" @confirm="handleResetConnect" >
            <template #reference>
                <a href="javascript:;" class="a-line">
                    <el-icon size="16"><SuccessFilled  /></el-icon><span>{{state.url}}</span>
                </a>
            </template>
        </el-popconfirm>
    </div>
</template>
<script>
import {injectGlobalData} from '@/provide'
import { computed, reactive} from 'vue';
import { initWebsocket,closeWebsocket,websocketState } from '@/apis/request'
import {SuccessFilled} from '@element-plus/icons-vue'

export default {
    components:{SuccessFilled},
    props:['config'],
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({
            url:websocketState.url,
            connected:computed(()=>globalData.value.api.connected),
        })
        const handleResetConnect = () => {
            localStorage.setItem('api-cache', '');
            sessionStorage.setItem('api-cache', '');
            window.location.reload();
        }
        const handleShow = ()=>{
            closeWebsocket();
            initWebsocket(`ws${window.location.protocol === "https:" ? "s" : ""}://${window.location.hostname}:12345`,'snltty');
        }

        return {state,handleShow,handleResetConnect};
    }
}
</script>
<style lang="stylus" scoped>
.status-api-wrap{
    &.connected {
       a{color:green;}
    }  
    a{
        color:#333;
        .el-icon{vertical-align: sub;margin-right:.4rem;}
        span{display: inline-flex;align-items: center;line-height: 1;}
    }
}

</style>