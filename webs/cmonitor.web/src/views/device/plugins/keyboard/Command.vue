<template>
    <div class="item">
        <div class="subitem">
            <span class="label">安全选项</span>
            <el-button @click="handleSas">Ctrl+Alt+Delete</el-button>
        </div>
    </div>

</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import { ctrlAltDelete } from '@/apis/keyboard';
export default {
    pluginName:'keyboard',
    setup() {

        const pluginState = injectPluginState();
        const handleSas = () => {
            handleSendCommand('确定发送ctrl+alt+delete吗？', ctrlAltDelete);
        }
        const handleSendCommand = (desc, command) => {
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                const names = pluginState.value.command.devices.map(c => c.MachineName);
                command(names).then((res) => {
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

        return { pluginState, handleSas }
    }
}
</script>

<style lang="stylus" scoped></style>