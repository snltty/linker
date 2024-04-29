<template>
    <el-button size="small" plain dark @click="handleMessage">窗口<el-icon>
            <Monitor />
        </el-icon>
    </el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'cmonitor.plugin.active.',
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleMessage = () => {
            pluginState.value.activeWindow.devices = globalData.value.devices;
            pluginState.value.activeWindow.showChoose = true;
        }

        return { handleMessage }
    }
}
</script>

<style lang="stylus" scoped></style>