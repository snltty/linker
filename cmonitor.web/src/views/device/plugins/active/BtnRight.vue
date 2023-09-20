<template>
    <a href="javascript:;">
        <span class="window" @click="handleWindow">
            <el-icon>
                <Monitor />
            </el-icon>
        </span>
        <p class="window-value">{{data.ActiveWindow.Count}}</p>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleWindow = () => {
            pluginState.value.activeWindow.devices = [props.data];
            pluginState.value.activeWindow.showChoose = true;
        }

        return {
            data: props.data,
            handleWindow
        }
    }
}
</script>

<style lang="stylus" scoped>
a {
    position: relative;
}

.window-value {
    color: #fff;
    text-shadow: 1px 1px 1px rgba(0, 0, 0, 255);
    font-size: 1.4rem;
    position: absolute;
    right: 110%;
    top: 50%;
    transform: translateY(-50%);
}

span.window {
    position: relative;
    height: 100%;
    display: block;

    .el-icon {
        font-size: 2rem;
    }
}
</style>