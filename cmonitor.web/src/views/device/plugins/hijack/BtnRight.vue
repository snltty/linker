<template>
    <a href="javascript:;" @click="handleRule">
        <el-icon>
            <Umbrella />
        </el-icon>
        <span class="value">{{data.Hijack.Count}}</span>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleRule = () => {

            pluginState.value.hijack.devices = [props.data];
            pluginState.value.hijack.showRules = true;
        }

        return { data: props.data, handleRule }
    }
}
</script>

<style lang="stylus" scoped>
a {
    position: relative;

    span.value {
        position: absolute;
        right: 110%;
        top: 50%;
        transform: translateY(-50%);
        color: rgba(255, 255, 255, 255);
        text-shadow: 1px 1px 1px rgba(0, 0, 0, 255);
        font-size: 1.4rem;
    }
}
</style>