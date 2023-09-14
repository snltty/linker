<template>
    <a href="javascript:;" @click="handleDevices">
        <span><el-icon>
                <Grid />
            </el-icon>设备</span>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    sort: 0,
    setup() {

        const pluginState = injectPluginState();
        const handleDevices = () => {
            pluginState.value.device.showDevices = true;
        }

        return {
            handleDevices
        }
    }
}
</script>

<style lang="stylus" scoped>
span {
    display: inline-flex;
    line-height: 1;
}
</style>