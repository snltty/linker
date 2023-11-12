<template>
    <div>
        <KeyBoard v-if="pluginState.keyboard.showKeyBoard" v-model="pluginState.keyboard.showKeyBoard"></KeyBoard>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
import KeyBoard from './KeyBoard.vue'
export default {
    components: { KeyBoard },
    setup() {
        const pluginState = injectPluginState();

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>