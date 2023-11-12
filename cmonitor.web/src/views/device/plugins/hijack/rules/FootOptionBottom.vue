<template>
    <el-button size="small" plain dark @click="handleRule">网络<el-icon>
            <Umbrella />
        </el-icon></el-button>
</template>

<script>
import { injectPluginState } from '@/views/device/provide';
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {
        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleRule = () => {
            pluginState.value.hijack.devices = globalData.value.devices;
            pluginState.value.hijack.showRules = true;
        }
        return {
            handleRule
        }
    }
}
</script>

<style lang="stylus" scoped></style>