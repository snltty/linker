<template>
    <div class="item">
        <div class="subitem">
            <span class="label">亮屏息屏</span>
            <el-button @click="handleDisplay(true)">亮屏</el-button>
            <el-button @click="handleDisplay(false)">息屏</el-button>
        </div>
        <div class="subitem">
            <span class="label">屏幕共享</span>
            <el-button @click="handleShare">开始</el-button>
        </div>
    </div>
    <el-dialog title="选择分享设备" destroy-on-close v-model="pluginState.screen.showShare" center align-center width="94%">
        <div class="t-c">
            <el-select v-model="pluginState.screen.device" placeholder="选择分享设备" size="large">
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
import { screenDisplay, screenShare } from '../../../../apis/screen'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalState = injectGlobalData();
        const handleDisplay = (state) => {
            const names = pluginState.value.command.devices.map(c => c.MachineName);
            screenDisplay(names, state).then((res) => {
                if (res) {
                    ElMessage.success('操作成功');
                } else {
                    ElMessage.error('操作失败');
                }
            }).catch(() => {
                ElMessage.error('操作失败');
            });
        }

        const handleShare = () => {
            pluginState.value.screen.showShare = true;
        }
        const handleCancel = () => {
            pluginState.value.screen.showShare = false;
        }
        const handleConfirm = () => {
            screenShare(pluginState.value.screen.device, pluginState.value.command.devices.map(c => c.MachineName)).then(() => {
                handleCancel();
                ElMessage.success('操作成功');
                pluginState.value.screen.shareUpdateFlag = Date.now();
            }).catch((e) => {
                console.log(e);
                ElMessage.error('操作失败');
            });
        }

        return { pluginState, globalState, handleDisplay, handleShare, handleCancel, handleConfirm }
    }
}
</script>

<style lang="stylus" scoped></style>