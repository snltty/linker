<template>
    <div class="active">
        <ActiveTimes v-if="pluginState.activeWindow.showTimes" v-model="pluginState.activeWindow.showTimes"></ActiveTimes>
        <Windows v-if="pluginState.activeWindow.showWindows" v-model="pluginState.activeWindow.showWindows"></Windows>
        <ChooseDig v-if="pluginState.activeWindow.showChoose" v-model="pluginState.activeWindow.showChoose"></ChooseDig>
        <AddWindow v-if="pluginState.activeWindow.showAddWindow" v-model="pluginState.activeWindow.showAddWindow"></AddWindow>
        <ActiveWindows v-if="pluginState.activeWindow.showActiveWindows" v-model="pluginState.activeWindow.showActiveWindows"></ActiveWindows>
    </div>
</template>

<script>
import ActiveTimes from './ActiveTimes.vue'
import AddWindow from './AddWindow.vue'
import ActiveWindows from './ActiveWindows.vue'
import Windows from './windows/Index.vue'
import ChooseDig from './ChooseDig.vue'
import { injectPluginState } from '../../provide'
export default {
    components: { ActiveTimes, Windows, ChooseDig, AddWindow, ActiveWindows },
    pluginName:'cmonitor.plugin.active.',
    setup() {
        const pluginState = injectPluginState();
        return {
            pluginState
        }
    }
}
</script>

<style lang="stylus" scoped></style>