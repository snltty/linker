<template>
    <el-button size="small" plain dark @click="handleMode">模式<el-icon>
            <Help />
        </el-icon></el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'cmonitor.plugin.modes.',
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleMode = () => {
            pluginState.value.modes.devices = globalData.value.devices;
            pluginState.value.modes.showModes = true;
        }

        return {
            handleMode
        }
    }
}
</script>

<style lang="stylus" scoped></style>