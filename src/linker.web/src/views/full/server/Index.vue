<template>
    <div class="servers-wrap" >
        <el-tabs type="border-card" style="width:100%" v-model="state.tab">
            <el-tab-pane label="信标服务器" name="signin" v-if="hasConfig">
                <SignInServers></SignInServers>
            </el-tab-pane>
            <el-tab-pane label="分组设置" name="groups" v-if="hasGroup">
                <Groups></Groups>
            </el-tab-pane>
            <el-tab-pane label="配置同步" name="async" v-if="hasSync">
                <Async></Async>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>
<script>
import { computed, onMounted, reactive } from 'vue';
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
        const hasGroup = computed(()=>globalData.value.hasAccess('Group'));

        const state = reactive({
            tab:'signin'
        });
        onMounted(()=>{
            state.tab =hasConfig.value ? 'signin' : hasGroup.value ? 'groups' : 'async';
        });

        return {
            state,hasConfig,hasSync,hasGroup
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