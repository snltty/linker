<template>
    <a href="javascript:;">
        <span class="volume" @click="handleVolume">
            <template v-if="data.Volume.Mute">
                <el-icon>
                    <Mute />
                </el-icon>
            </template>
            <template v-else>
                <el-icon>
                    <Microphone />
                </el-icon>
                <div class="volume-bg" :style="{height:`${data.Volume.Value}%`}">
                    <el-icon class="value">
                        <Microphone />
                    </el-icon>
                </div>
            </template>
        </span>
        <p class="volume-value">{{Math.floor(data.Volume.Value)}}</p>
    </a>
</template>

<script>
import { injectPluginState } from '../../provide'
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleVolume = () => {
            pluginState.value.volume.devices = [props.data];
            pluginState.value.volume.showVolumeSingle = true;
        }

        return {
            data: props.data,
            handleVolume
        }
    }
}
</script>

<style lang="stylus" scoped>
a {
    position: relative;
}

.volume-value {
    color: #fff;
    text-shadow: 1px 1px 1px rgba(0, 0, 0, 255);
    font-size: 1.4rem;
    position: absolute;
    right: 110%;
    top: 50%;
    transform: translateY(-50%);
}

span.volume {
    position: relative;
    height: 100%;
    display: block;

    .el-icon {
        font-size: 2rem;
    }

    .volume-bg {
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