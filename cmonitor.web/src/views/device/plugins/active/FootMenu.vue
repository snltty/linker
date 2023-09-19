<template>
    <a href="javascript:;" @click="handleWindows">
        <span>
            <el-icon>
                <Monitor />
            </el-icon>窗口
        </span>
    </a>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    sort: 1,
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
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
span {
    display: inline-flex;
    line-height: 1;
}
</style>