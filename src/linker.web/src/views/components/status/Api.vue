<template>
    <div v-if="config" class="status-api-wrap" :class="{connected:connected}">
        <el-popconfirm 
        :confirm-button-text="$t('status.apiClear')" 
        :cancel-button-text="$t('status.apiAlter')" 
        :title="$t('status.apiAlterConfirm')" @cancel="handleShow" @confirm="handleResetConnect" >
            <template #reference>
                <a href="javascript:;">
                    <el-icon size="16"><Flag /></el-icon>
                    <span>{{$t('status.api')}}</span>
                </a>
            </template>
        </el-popconfirm>
    </div>
</template>
<script>
import {injectGlobalData} from '@/provide'
import { computed} from 'vue';
import { initWebsocket,closeWebsocket } from '@/apis/request'
import {Flag} from '@element-plus/icons-vue'
export default {
    components:{Flag},
    props:['config'],
    setup(props) {
        const globalData = injectGlobalData();
        const connected = computed(()=>globalData.value.api.connected);
        const handleResetConnect = () => {
            localStorage.setItem('api-cache', '');
            sessionStorage.setItem('api-cache', '');
            window.location.reload();
        }
        const handleShow = ()=>{
            closeWebsocket();
            initWebsocket(`ws${window.location.protocol === "https:" ? "s" : ""}://${window.location.hostname}:12345`,'snltty');
        }
        return {config:props.config,connected,handleShow,handleResetConnect};
    }
}
</script>
<style lang="stylus" scoped>
.status-api-wrap{
    padding-right:1rem;
    &.connected {
       a{color:green;font-weight:bold;}
    }  
    a{
        color:#333;
        .el-icon{
            vertical-align:sub;
        }
    }
}

</style>