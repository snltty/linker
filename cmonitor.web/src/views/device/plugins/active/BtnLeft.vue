<template>
    <a href="javascript:;" @click="handleTimes">
        <el-icon>
            <DataLine />
        </el-icon>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleTimes = () => {
            pluginState.value.activeWindow.items = [props.data];
            pluginState.value.activeWindow.showTimes = true;
        }
        return {
            handleTimes
        }
    }
}
</script>

<style lang="stylus" scoped></style>