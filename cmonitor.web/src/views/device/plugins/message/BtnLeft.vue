<template>
    <a href="javascript:;" @click="handleMessage">
        <el-icon>
            <Bell />
        </el-icon>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {
        const pluginState = injectPluginState();
        const handleMessage = () => {
            pluginState.value.message.items = [props.data];
            pluginState.value.message.showMessage = true;
        }
        return {
            handleMessage
        }
    }
}
</script>

<style lang="stylus" scoped></style>