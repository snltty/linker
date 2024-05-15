<template>
    <div class="item">
        <div class="subitem">
            <span class="label">屏幕壁纸</span>
            <el-button @click="handleOpenWallpaper">打开</el-button>
            <el-button @click="handleCloseWallpaper">关闭</el-button>
        </div>
    </div>

</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { wallpaperUpdate } from '@/apis/wallpaper';
export default {
    pluginName:'wallpaper',
    setup() {

        const pluginState = injectPluginState();
        const handleOpenWallpaper = () => {
            handleSendCommand('确定打开壁纸吗？', wallpaperFunc, true);
        }
        const handleCloseWallpaper = () => {
            handleSendCommand('确定关闭壁纸吗？', wallpaperFunc, false);
        }

        const wallpaperFunc = (names, value) => {
            return wallpaperUpdate(names, value, `http://${window.location.hostname}:${window.location.port}/bg.jpg`);
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

        return { pluginState, handleOpenWallpaper, handleCloseWallpaper }
    }
}
</script>

<style lang="stylus" scoped></style>