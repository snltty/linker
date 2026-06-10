<template>
    <div>
        <el-dialog append-to=".app-wrap" v-model="state.show" :title="$t('install.init')" width="80rem" top="2vh">
            <div>
                <el-tabs type="border-card">
                    <el-tab-pane :label="$t('install.input')">
                        <Input></Input>
                    </el-tab-pane>
                    <el-tab-pane :label="$t('install.copy')">
                        <Copy></Copy>
                    </el-tab-pane>
                    <el-tab-pane :label="$t('install.import')">
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
import Input from '../../../components/install/Input.vue'
import Copy from '../../../components/install/Copy.vue'
import Save from '../../../components/install/Save.vue'
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