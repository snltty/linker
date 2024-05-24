<template>
    <div class="status-api-wrap" :class="{connected:connected}">
        <a href="javascript:;" @click="handleResetConnect">
            <template v-if="connected">已连接本地管理接口</template>
            <template v-else>请连接管理接口</template>
        </a>
    </div>
</template>
<script>
import {computed} from 'vue'
import {useRoute,useRouter} from 'vue-router'
import {injectGlobalData} from '../../provide'
export default {
    setup(props) {
        const globalData = injectGlobalData();
        const connected = computed(()=>globalData.value.connected);
        const router = useRouter();
        const route = useRoute();

        const handleResetConnect = () => {
            localStorage.setItem('api-client', '');
            localStorage.setItem('apipsd-client', '');
            router.push({name:route.name});
            window.location.reload();
        }

        return {connected,handleResetConnect};
    }
}
</script>
<style lang="stylus" scoped>
.status-api-wrap{
    padding-right:2rem;
    a{color:#333;}
    span{border-radius:1rem;background-color:rgba(0,0,0,0.1);padding:0 .6rem;margin-left:.2rem}

    &.connected {
       a{color:green;font-weight:bold;}
       span{background-color:green;color:#fff;}
    }  
}

</style>