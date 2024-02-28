<template>
    <div class="item">
        <div class="subitem">
            <span class="label">连接网络</span>
            <el-button @click="handleNetworkConnect">连接网络设置</el-button>
        </div>
        <el-dialog class="options-center" title="选择网络" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="80%">
            <div class="wifi-wrap t-c">
                <div>
                    <el-select multiple v-model="state.checkWifis" placeholder="选择网络" size="large">
                        <el-option v-for="item in state.wifis" :key="item" :label="item" :value="item" />
                    </el-select>
                </div>
                <div style="padding-top:1rem;">
                    <el-checkbox v-model="state.autoConnect">断开自动重连</el-checkbox>
                </div>
            </div>
            <template #footer>
                <el-button @click="handleCancel" plain>取 消</el-button>
                <el-button type="success" @click="handleConfirm" plain>确 定</el-button>
            </template>
        </el-dialog>
    </div>

</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { exec } from '@/apis/command';
import { injectGlobalData } from '@/views/provide';
import { reactive } from 'vue';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();

        const state = reactive({
            show: true,
            wifis: ['xxx', 'xx1', 'aaaa', 'bbbbb', 'ccccccc'],
            checkWifis: [],
            autoConnect: true
        });

        const handleCancel = () => {
            state.show = false;
        }
        const handleConfirm = () => {
            state.show = false;
        }
        const handleNetworkConnect = () => {

        }

        return {
            pluginState, state, handleCancel, handleConfirm, handleNetworkConnect
        }
    }
}
</script>

<style lang="stylus" scoped></style>