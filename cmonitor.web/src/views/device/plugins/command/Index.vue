<template>
    <div>
        <ChooseDig v-if="pluginState.command.showCommand" v-model="pluginState.command.showCommand"></ChooseDig>
        <KeyBoard v-if="pluginState.command.showKeyBoard" v-model="pluginState.command.showKeyBoard"></KeyBoard>
        <el-dialog title="执行命令" destroy-on-close v-model="pluginState.command.showCloseSystem" center align-center width="94%">
            <div class="t-c">
                <el-button size="large" type="warning" plain @click="handleCloseSystem">强制关机</el-button>
                <el-button size="large" type="danger" plain @click="handleRebotSystem">强制重启</el-button>
            </div>
            <template #footer></template>
        </el-dialog>
    </div>
</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide'
import ChooseDig from './ChooseDig.vue'
import KeyBoard from './KeyBoard.vue'
import { exec } from '@/apis/command';
export default {
    components: { ChooseDig, KeyBoard },
    setup() {
        const pluginState = injectPluginState();


        const handleRebotSystem = () => {
            handleSendCommand('确定重启系统吗？', `shutdown -r -f -t 00`);
        }
        const handleCloseSystem = () => {
            handleSendCommand('确定关闭系统吗？', `shutdown -s -f -t 00`);
        }

        const handleSendCommand = (desc, command, value) => {
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                pluginState.value.command.showCloseSystem = false;
                const names = pluginState.value.command.items.map(c => c.MachineName);
                exec(names, [command]).then((res) => {
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

        return { pluginState, handleRebotSystem, handleCloseSystem }
    }
}
</script>

<style lang="stylus" scoped></style>