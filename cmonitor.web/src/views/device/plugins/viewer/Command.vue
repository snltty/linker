<template>
    <div class="item">
        <div class="subitem">
            <span class="label">屏幕共享</span>
            <el-button @click="handleShare">开始</el-button>
        </div>
    </div>
    <el-dialog title="选择分享设备" destroy-on-close v-model="pluginState.viewer.showShare" center align-center width="94%">
        <div class="t-c">
            <el-select v-model="pluginState.viewer.device" placeholder="选择分享设备" size="large">
                <el-option v-for="item in globalState.devices" :key="item.MachineName" :label="item.MachineName" :value="item.MachineName" />
            </el-select>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain @click="handleConfirm">确 定</el-button>
        </template>
    </el-dialog>

</template>

<script>
import { ElMessage } from 'element-plus';
import { injectPluginState } from '../../provide'
import { injectGlobalData } from '@/views/provide';
import { viewerUpdate } from '@/apis/viewer';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalState = injectGlobalData();

        const handleShare = () => {
            pluginState.value.viewer.showShare = true;
        }
        const handleCancel = () => {
            pluginState.value.viewer.showShare = false;
        }
        const handleConfirm = () => {
            const clients = pluginState.value.command.devices.map(c => c.MachineName);
            if (clients.length == 0) {
                ElMessage.error('请至少选择一个目标设备');
                return;
            }
            if (!pluginState.value.viewer.device) {
                ElMessage.error('请选择一个共享设备');
                return;
            }
            viewerUpdate({
                Open: true,
                Server: pluginState.value.viewer.device,
                Clients: clients,
            }).then(() => {
                pluginState.value.viewer.showShare = false;
                ElMessage.success('操作成功');
            }).catch((e) => {
                ElMessage.error('操作失败');
            });
        }

        return { pluginState, globalState, handleShare, handleCancel, handleConfirm }
    }
}
</script>

<style lang="stylus" scoped></style>