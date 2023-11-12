<template>
    <div class="item">
        <div class="subitem">
            <span class="label">操作系统</span>
            <el-button @click="handleRebotSystem">重启</el-button>
            <el-button @click="handleCloseSystem">关机</el-button>
        </div>
        <div class="subitem">
            <span class="label">系统桌面</span>
            <el-button @click="handleOpenDisktop">开启</el-button>
            <el-button @click="handleCloseDisktop">关闭</el-button>
        </div>
    </div>
</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { exec } from '@/apis/command';
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleRebotSystem = () => {
            handleSendCommand('确定重启系统吗？', `shutdown -r -f -t 00`);
        }
        const handleCloseSystem = () => {
            handleSendCommand('确定关闭系统吗？', `shutdown -s -f -t 00`);
        }
        const handleOpenDisktop = () => {
            handleSendCommand('确定开启桌面吗？', `start explorer.exe`);
        }
        const handleCloseDisktop = () => {
            handleSendCommand('确定关闭桌面吗？', `taskkill /f /t /im "explorer.exe"`);
        }

        const handleSendCommand = (desc, command, value) => {
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                const names = pluginState.value.command.devices.map(c => c.MachineName);
                const func = typeof command == 'string' ? exec(names, [command]) : command(names, value);
                func.then((res) => {
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

        return {
            pluginState, handleRebotSystem, handleCloseSystem, handleOpenDisktop, handleCloseDisktop
        }
    }
}
</script>

<style lang="stylus" scoped></style>