<template>
    <el-button size="small" plain dark @click="handleLight">亮度 <el-icon>
            <Sunny />
        </el-icon></el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleLight = () => {
            pluginState.value.light.items = globalData.value.devices;
            pluginState.value.light.showLight = true;
        }

        return {
            handleLight
        }
    }
}
</script>

<style lang="stylus" scoped></style>