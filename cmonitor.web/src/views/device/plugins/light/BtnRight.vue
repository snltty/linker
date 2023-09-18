<template>
    <a href="javascript:;">
        <span class="light" @click="handleLight">
            <el-icon>
                <Sunny />
            </el-icon>
            <div class="light-bg" :style="{height:`${data.Light.Value}%`}">
                <el-icon class="value">
                    <Sunny />
                </el-icon>
            </div>
        </span>
        <p class="light-value">{{data.Light.Value}}</p>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleLight = () => {
            pluginState.value.light.items = [props.data];
            pluginState.value.light.showLightSingle = true;
        }

        return {
            data: props.data,
            handleLight
        }
    }
}
</script>

<style lang="stylus" scoped>
a {
    position: relative;
}

.light-value {
    color: #fff;
    text-shadow: 1px 1px 1px rgba(0, 0, 0, 255);
    font-size: 1.4rem;
    position: absolute;
    right: 110%;
    top: 50%;
    transform: translateY(-50%);
}

span.light {
    position: relative;
    height: 100%;
    display: block;

    .el-icon {
        font-size: 2rem;
    }

    .light-bg {
        position: absolute;
        bottom: 2px;
        left: 0;
        height: 0%;
        width: 100%;
        overflow: hidden;

        .el-icon {
            color: green;
            position: absolute;
            left: 50%;
            transform: translateX(-50%);
            bottom: 0;
        }
    }
}
</style>