<template>
    <div>
    </div>
</template>

<script>
import { onMounted, watch } from 'vue';
import { injectPluginState } from '../../provide'
import { injectGlobalData } from '@/views/provide';
export default {
    components: {},
    setup() {

        const pluginState = injectPluginState();
        const globalState = injectGlobalData();
        watch(() => pluginState.value.viewer.shareUpdateFlag, () => { });

        onMounted(() => { })

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>