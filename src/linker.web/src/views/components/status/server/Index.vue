<template>
    <div class="status-server-wrap">
        <Groups :config="config"></Groups>
        <ServerVersion :config="config"></ServerVersion>
        <AccessShow value="Flow">
            <Flow v-if="config" :config="config"></Flow>
        </AccessShow>
    </div>
</template>
<script>
import { reactive } from 'vue';
import Groups from '../../groups/Index.vue';
import Flow from './Flow.vue';
import ServerVersion from './Version.vue';
export default {
    components:{Groups,Flow,ServerVersion},
    props:['config'],
    setup(props) {

        const state = reactive({
            show: false,
            loading: false
        });

        return {
         config:props.config,state
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