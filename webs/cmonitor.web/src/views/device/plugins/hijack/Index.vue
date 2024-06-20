<template>
    <div>
        <ChooseDig v-if="pluginState.hijack.showRules" v-model="pluginState.hijack.showRules"></ChooseDig>
        <RuleSetting v-if="pluginState.hijack.showRuleSetting" v-model="pluginState.hijack.showRuleSetting"></RuleSetting>
        <ProcessSetting v-if="pluginState.hijack.showProcessSetting" v-model="pluginState.hijack.showProcessSetting"></ProcessSetting>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
import ChooseDig from './ChooseDig.vue'
import RuleSetting from './rules/Index.vue'
import ProcessSetting from './process/Index.vue'
export default {
    pluginName:'hijack',
    components: { ChooseDig, RuleSetting, ProcessSetting },
    setup() {
        const pluginState = injectPluginState();

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>