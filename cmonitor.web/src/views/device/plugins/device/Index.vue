<template>
    <div>
        <ChooseDig v-if="pluginState.device.showDevices" v-model="pluginState.device.showDevices"></ChooseDig>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
import ChooseDig from './ChooseDig.vue'
export default {
    pluginName:'cmonitor.plugin.device.',
    components: { ChooseDig },
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>