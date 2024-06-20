<template>
    <div class="item">
        <div class="subitem">
            <span class="label">屏幕锁定</span>
            <el-button @click="handleLockScreen">锁定</el-button>
            <el-button @click="handleUnLockScreen">解锁</el-button>
        </div>
        <div class="subitem">
            <span class="label">系统锁定</span>
            <el-button @click="handleLockSystem">锁定(WIN+L)</el-button>
        </div>
    </div>

</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { llockScreen, lockSystem } from '../../../../apis/llock'
export default {
    pluginName:'llock',
    setup() {

        const pluginState = injectPluginState();
        const handleLockScreen = () => {
            handleSendCommand('确定锁定屏幕吗？', llockScreen, true);
        }
        const handleUnLockScreen = () => {
            handleSendCommand('确定解锁屏幕吗？', llockScreen, false);
        }
        const handleLockSystem = () => {
            handleSendCommand('确定解锁系统吗？', lockSystem, true);
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

        return { pluginState, handleLockScreen, handleUnLockScreen, handleLockSystem }
    }
}
</script>

<style lang="stylus" scoped></style>