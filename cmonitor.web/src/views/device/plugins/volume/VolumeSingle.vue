<template>
    <el-dialog class="volume-dialog" title="调节音量" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="slider-wrap flex flex-column">
            <div class="silder flex flex-1">
                <div class="flex-1">
                    <el-slider @change="handleChangeVolume" v-model="state.volume" />
                </div>
            </div>
            <div class="gif" v-if="state.showRecord">
                <img src="@/assets/volume.gif">
            </div>
            <!-- <div class="btn">
                <el-button @touchstart="handleMicMouseDown" @touchend="handleMicMouseUp" :icon="Mic" size="large" round>发送语音</el-button>
            </div> -->
        </div>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import { setVolume } from '../../../../apis/volume'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
import { Mic } from '@element-plus/icons-vue'
export default {
    props: ['modelValue', 'items'],
    emits: ['update:modelValue'],
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.volume.items),
            loading: false,
            volume: pluginState.value.volume.items[0].Volume.Value,
            showRecord: false
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleCancel = () => {
            state.show = false;
        }
        const handleChangeVolume = () => {
            setVolume(state.items.map(c => c.MachineName), state.volume / 100);
        }

        const handleMicMouseDown = () => {
            state.showRecord = true;
        }
        const handleMicMouseUp = () => {
            state.showRecord = false;
        }


        return {
            Mic, state, globalData, handleCancel, handleChangeVolume, handleMicMouseDown, handleMicMouseUp
        }
    }
}
</script>
<style lang="stylus" scoped>
.volume-dialog {
    -webkit-touch-callout: none;
    -webkit-user-select: none;
    -khtml-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
}

.volume {
    font-size: 2rem;
    padding-left: 1rem;

    .value {
        font-size: 1.4rem;
    }
}

.slider-wrap {
    text-align: center;

    .gif {
        img {
            width: 100%;
        }
    }

    // height: 10rem;
    .silder {
        padding: 0rem 4rem 2rem 4rem;
    }

    .btn {
        padding-top: 2rem;
    }
}
</style>