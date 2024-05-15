<template>
    <div>
        <Light v-if="pluginState.light.showLight" v-model="pluginState.light.showLight"></Light>
        <LightSingle v-if="pluginState.light.showLightSingle" v-model="pluginState.light.showLightSingle"></LightSingle>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
import Light from './Light.vue'
import LightSingle from './LightSingle.vue'
export default {
    pluginName:'light',
    components: { Light, LightSingle },
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>