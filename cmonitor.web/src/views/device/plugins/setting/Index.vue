<template>
    <div>
        <ChooseDig v-if="pluginState.setting.showSetting" v-model="pluginState.setting.showSetting"></ChooseDig>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
import ChooseDig from './ChooseDig.vue'
export default {
    components: { ChooseDig },
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>