<template>
    <div class="status-server-wrap">
        <ServerConfig :config="config"></ServerConfig>
        <ServerVersion :config="config"></ServerVersion>
        <ServerFlow :config="config"></ServerFlow>
    </div>
</template>
<script>
import { reactive } from 'vue';
import ServerConfig from './ServerConfig.vue';
import ServerFlow from './ServerFlow.vue';
import ServerVersion from './ServerVersion.vue';
export default {
    components:{ServerConfig,ServerFlow,ServerVersion},
    props:['config'],
    setup(props) {

        const state = reactive({
            show: false,
            loading: false
        });

        return {
         config:props.config,  state
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