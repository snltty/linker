<template>
    <el-button size="small" plain dark @click="handleCommand">命令<el-icon>
            <ScaleToOriginal />
        </el-icon></el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide'
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleCommand = () => {
            pluginState.value.command.devices = globalData.value.devices;
            pluginState.value.command.showCommand = true;
        }

        return {
            handleCommand
        }
    }
}
</script>

<style lang="stylus" scoped></style>