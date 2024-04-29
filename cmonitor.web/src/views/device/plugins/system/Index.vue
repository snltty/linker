<template>
    <div>
        <SystemOptions v-if="pluginState.system.showSystemOptions" v-model="pluginState.system.showSystemOptions"></SystemOptions>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
import SystemOptions from './SystemOptions.vue'
export default {
    pluginName:'cmonitor.plugin.system.',
    components: { SystemOptions },
    setup() {
        const pluginState = injectPluginState();

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>