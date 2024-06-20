<template>
    <div class="item">
        <div class="subitem">
            <span class="label">亮屏息屏</span>
            <el-button @click="handleDisplay(true)">亮屏</el-button>
            <el-button @click="handleDisplay(false)">息屏</el-button>
        </div>
    </div>
</template>

<script>
import { ElMessage } from 'element-plus';
import { injectPluginState } from '../../provide'
import { screenDisplay } from '../../../../apis/display'
import { injectGlobalData } from '@/views/provide';
export default {
    pluginName:'screen',
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

        return { pluginState, globalState, handleDisplay }
    }
}
</script>

<style lang="stylus" scoped></style>