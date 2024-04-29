<template>
    <div class="item">
        <div class="subitem">
            <span class="label">系统静音</span>
            <el-button @click="handleOpenMute">静音</el-button>
            <el-button @click="handleCloseMute">取消</el-button>
        </div>
    </div>

</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { setVolumeMute } from '@/apis/volume';
export default {
    pluginName:'cmonitor.plugin.volume.',
    setup() {

        const pluginState = injectPluginState();
        const handleOpenMute = () => {
            handleSendCommand('确定设置静音吗？', setVolumeMute, true);
        }
        const handleCloseMute = () => {
            handleSendCommand('确定取消静音吗？', setVolumeMute, false);
        }
        const handleSendCommand = (desc, command, value) => {
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                const names = pluginState.value.command.devices.map(c => c.MachineName);
                command(names, value).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败');
                    }
                }).catch(() => {
                    ElMessage.error('操作失败');
                });
            }).catch(() => { });
        }

        return { pluginState, handleOpenMute, handleCloseMute }
    }
}
</script>

<style lang="stylus" scoped></style>