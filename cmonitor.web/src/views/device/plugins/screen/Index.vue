<template>
    <div>
    </div>
</template>

<script>
import { onMounted, watch } from 'vue';
import { injectPluginState } from '../../provide'
import { screenGetShare } from '@/apis/screen';
import { injectGlobalData } from '@/views/provide';
export default {
    components: {},
    setup() {

        const pluginState = injectPluginState();
        const globalState = injectGlobalData();
        watch(() => pluginState.value.screen.shareUpdateFlag, () => {
            getShareNames();

        });
        const getShareNames = () => {
            screenGetShare().then((names) => {
                globalState.value.devices.filter(c => names.indexOf(c.MachineName) >= 0).map(c => {
                    c.Screen.share = true;
                });
                globalState.value.devices.filter(c => names.indexOf(c.MachineName) < 0).map(c => {
                    c.Screen.share = false;
                });
            });
        }

        onMounted(() => {
            getShareNames();
        })

        return { pluginState }
    }
}
</script>

<style lang="stylus" scoped></style>