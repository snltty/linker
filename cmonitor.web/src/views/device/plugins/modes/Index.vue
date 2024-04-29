<template>
    <div>
        <ChooseDig v-if="pluginState.modes.showModes" v-model="pluginState.modes.showModes"></ChooseDig>
        <SettingDig v-if="pluginState.modes.showModesSetting" v-model="pluginState.modes.showModesSetting"></SettingDig>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
import ChooseDig from './ChooseDig.vue'
import SettingDig from './SettingDig.vue'
export default {
    pluginName:'cmonitor.plugin.modes.',
    components: { ChooseDig, SettingDig },
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>