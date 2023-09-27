<template>
    <a href="javascript:;" @click="handleDevices">
        <el-icon>
            <Grid />
        </el-icon>
        <span>设备</span>
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
.el-icon {
    vertical-align: middle;
    margin-top: -3px;
}

span {
    display: inline-flex;
    line-height: 1;
}
</style>