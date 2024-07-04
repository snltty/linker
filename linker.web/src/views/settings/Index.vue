<template>
    <div class="servers-wrap">
        <div class="pdb-6 t-c">
            <el-checkbox v-model="settingState.sync" label="自动同步更改"  />
            <el-button type="primary" @click="handleSave">立即同步</el-button>
        </div>
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
import { computed, provide, reactive, ref } from 'vue';
import { injectGlobalData } from '@/provide';
export default {
    components:{},
    setup(props) {
        const files = require.context('./', true, /.+\.vue/);
        const settingComponents = files.keys().filter(c=>c != './Index.vue').map(c => files(c).default).sort((a,b)=>a.order-b.order);

        const globalData = injectGlobalData();
        const state = reactive({
            tab:settingComponents[0].name,
            connected:computed(()=>globalData.value.connected && globalData.value.configed),
        });

        const settingState = ref({sync:true});
        provide('setting',settingState);
        const handleSave = ()=>{
        }

        return {
            state,settingState,settingComponents,handleSave
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