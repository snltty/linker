<template>
    <div class="servers-wrap">
        <el-tabs type="border-card" style="width:100%" v-model="state.tab">
            <template v-if="state.connected" v-for="(item,index) in settingComponents" :key="index">
                <el-tab-pane :label="item.label" :name="item.name">
                    <component :is="item"></component>
                </el-tab-pane>
            </template>
        </el-tabs>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import { injectGlobalData } from '@/provide';
export default {
    components:{},
    setup(props) {

        const excludes = ['./Index.vue','./Version.vue']

        const files = require.context('./', true, /.+\.vue/);
        const settingComponents = files.keys().filter(c=>excludes.includes(c)==false).map(c => files(c).default).sort((a,b)=>a.order-b.order);
        const globalData = injectGlobalData();
        const state = reactive({
            tab:settingComponents[0].name,
            connected:computed(()=>globalData.value.api.connected && globalData.value.config.configed),
        });
        return {
            state,settingComponents
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