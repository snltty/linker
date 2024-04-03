<template>
    <div>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide';
export default {
    components: {},
    setup() {
        const pluginState = injectPluginState();
        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>