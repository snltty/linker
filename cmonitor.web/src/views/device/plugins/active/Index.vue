<template>
    <div class="active">
        <ActiveTimes v-if="pluginState.activeWindow.showTimes" v-model="pluginState.activeWindow.showTimes"></ActiveTimes>
        <FileNames v-if="pluginState.activeWindow.showFileNames" v-model="pluginState.activeWindow.showFileNames"></FileNames>
        <ChooseDig v-if="pluginState.activeWindow.showChoose" v-model="pluginState.activeWindow.showChoose"></ChooseDig>
    </div>
</template>

<script>
import ActiveTimes from './ActiveTimes.vue'
import FileNames from './FileNames.vue'
import ChooseDig from './ChooseDig.vue'
import { injectPluginState } from '../../provide'
export default {
    components: { ActiveTimes, FileNames, ChooseDig },
    setup() {
        const pluginState = injectPluginState();
        return {
            pluginState
        }
    }
}
</script>

<style lang="stylus" scoped></style>