<template>
    <el-button size="small" plain dark @click="handleVolume">音量<el-icon>
            <Microphone />
        </el-icon></el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'volume',
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleVolume = () => {
            pluginState.value.volume.devices = globalData.value.devices;
            pluginState.value.volume.showVolume = true;
        }

        return {
            handleVolume
        }
    }
}
</script>

<style lang="stylus" scoped></style>