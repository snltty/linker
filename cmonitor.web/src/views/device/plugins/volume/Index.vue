<template>
    <div>
        <Volume v-if="pluginState.volume.showVolume" v-model="pluginState.volume.showVolume"></Volume>
        <VolumeSingle v-if="pluginState.volume.showVolumeSingle" v-model="pluginState.volume.showVolumeSingle"></VolumeSingle>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
import Volume from './Volume.vue'
import VolumeSingle from './VolumeSingle.vue'
export default {
    pluginName:'cmonitor.plugin.volume.',
    components: { Volume, VolumeSingle },
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>