<template>
    <div class="servers-wrap" v-if="hasConfig">
        <el-tabs type="border-card" style="width:100%" v-model="state.tab">
            <el-tab-pane label="信标服务器" name="signin">
                <SignInServers></SignInServers>
            </el-tab-pane>
            <el-tab-pane label="分组设置" name="groups">
                <Groups></Groups>
            </el-tab-pane>
            <el-tab-pane label="配置同步" name="async" v-if="hasSync">
                <Async></Async>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import { injectGlobalData } from '@/provide';
import SignInServers from './SignInServers.vue';
import Groups from './Groups.vue';
import Async from './Async.vue';
export default {
    components:{SignInServers,Groups,Async},
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