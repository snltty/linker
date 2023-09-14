<template>
    <a href="javascript:;" @click="handleCommand">
        <el-icon>
            <Position />
        </el-icon>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleCommand = () => {
            pluginState.value.command.items = [props.data];
            pluginState.value.command.showCommand = true;
        }

        return {
            handleCommand
        }
    }
}
</script>

<style lang="stylus" scoped></style>