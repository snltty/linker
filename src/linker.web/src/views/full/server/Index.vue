<template>
    <div class="servers-wrap" >
        <el-tabs type="border-card" style="width:100%" v-model="state.tab">
            <el-tab-pane :label="$t('server.messenger')" name="signin" v-if="hasConfig">
                <SignInServers></SignInServers>
            </el-tab-pane>
            <el-tab-pane :label="$t('server.sync')" name="async" v-if="hasSync">
                <Async></Async>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>
<script>
import { computed,  reactive } from 'vue';
import { injectGlobalData } from '@/provide';
import SignInServers from './SignInServers.vue';
import Async from './Async.vue';
export default {
    components:{SignInServers,Async},
    setup(props) {

        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config'))
        const hasSync = computed(()=>globalData.value.hasAccess('Sync'));
        const state = reactive({
            tab:'signin'
        });
        return {
            state,hasConfig,hasSync
        }
    }
}
</script>
<style lang="stylus" scoped>
.servers-wrap{
    padding:1rem
    font-size:1.3rem;
    color:#555;
    a{color:#333;}
}
.el-checkbox{
    vertical-align:middle;
    margin-right:1rem;
}

</style>