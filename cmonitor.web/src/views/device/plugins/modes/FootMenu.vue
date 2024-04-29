<template>
    <a href="javascript:;" @click="handleSetting">
        <el-icon>
            <Help />
        </el-icon>
        <span>模式</span>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    pluginName:'cmonitor.plugin.modes.',
    sort: 0,
    setup() {

        const pluginState = injectPluginState();
        const handleSetting = () => {
            pluginState.value.modes.showModesSetting = true;
        }

        return {
            handleSetting
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