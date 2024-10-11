<template>
    <div class="status-server-wrap">
        <ServerConfig :config="config"></ServerConfig>
        <ServerVersion :config="config"></ServerVersion>
        <ServerFlow v-if="config && hasFlow" :config="config"></ServerFlow>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import ServerConfig from './ServerConfig.vue';
import ServerFlow from './ServerFlow.vue';
import ServerVersion from './ServerVersion.vue';
import { injectGlobalData } from '@/provide';
export default {
    components:{ServerConfig,ServerFlow,ServerVersion},
    props:['config'],
    setup(props) {

        const globalData = injectGlobalData();
        const hasFlow = computed(()=>globalData.value.hasAccess('Flow')); 

        const state = reactive({
            show: false,
            loading: false
        });

        return {
         config:props.config,hasFlow,  state
        }
    }
}
</script>
<style lang="stylus" scoped>
.status-server-wrap{
    position:relative;
    padding-right:.5rem;
    a{color:#333;}
    a+a{margin-left:.6rem;}
    .el-icon{
        vertical-align:text-bottom;
    }
}

</style>