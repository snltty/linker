<template>
    <div>
        <el-dialog v-model="state.show" title="初始化配置" width="700" top="2vh">
            <div>
                <el-tabs type="border-card">
                    <el-tab-pane label="手动输入">
                        <Input></Input>
                    </el-tab-pane>
                    <el-tab-pane label="粘贴配置">
                        <Copy></Copy>
                    </el-tab-pane>
                    <el-tab-pane label="在线导入">
                        <Save></Save>
                    </el-tab-pane>
                </el-tabs>
            </div>
        </el-dialog>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { reactive} from 'vue';
import Input from './Input.vue'
import Copy from './Copy.vue'
import Save from './Save.vue'
export default {
    components: { Input,Copy,Save},
    setup(props) {

        const globalData = injectGlobalData();   
        const state = reactive({
            show: globalData.value.config.Common.Install == false
        });  

        return { state,globalData};
    }
}
</script>
<style lang="stylus" scoped>

</style>