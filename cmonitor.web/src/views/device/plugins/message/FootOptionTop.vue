<template>
    <el-button size="small" plain dark @click="handleMessage">提醒<el-icon>
            <Bell />
        </el-icon>
    </el-button>
</template>

<script>
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    setup() {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const handleMessage = () => {
            pluginState.value.message.items = globalData.value.devices;
            pluginState.value.message.showMessage = true;
        }

        return { handleMessage }
    }
}
</script>

<style lang="stylus" scoped></style>