<template>
    <div class="item">
        <div class="subitem">
            <span class="label">命令终端</span>
            <el-button @click="handleTerm">打开</el-button>
        </div>
    </div>

</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    pluginName:'command',
    setup() {

        const pluginState = injectPluginState();
        const handleTerm = () => {
            pluginState.value.command.showTerm = true;
        }

        return { handleTerm }
    }
}
</script>

<style lang="stylus" scoped></style>