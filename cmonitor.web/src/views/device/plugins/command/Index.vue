<template>
    <div>
        <ChooseDig v-if="pluginState.command.showCommand" v-model="pluginState.command.showCommand"></ChooseDig>
        <Term v-if="pluginState.command.showTerm" v-model="pluginState.command.showTerm"></Term>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
import ChooseDig from './ChooseDig.vue'
import Term from './Term.vue'
export default {
    components: { ChooseDig, Term },
    setup() {
        const pluginState = injectPluginState();

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>