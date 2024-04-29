<template>
    <a href="javascript:;" @click="handleWindows">
        <el-icon>
            <Monitor />
        </el-icon>
        <span>窗口</span>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide';
export default {
    sort: 1,
    pluginName:'cmonitor.plugin.active.',
    setup() {

        const pluginState = injectPluginState();
        const handleWindows = () => {
            pluginState.value.activeWindow.showWindows = true;
        }

        return {
            handleWindows
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