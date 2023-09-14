<template>
    <el-dialog title="调节音量" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="slider-wrap flex flex-column">
            <div class="silder flex flex-1">
                <div class="flex-1">
                    <el-slider @change="handleChangeVolume" v-model="state.volume" />
                </div>
            </div>
        </div>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import { setVolume } from '../../../../apis/volume'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    props: ['modelValue', 'items'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.volume.items),
            loading: false,
            volume: pluginState.value.volume.items[0].Volume.Value
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


        return {
            state, globalData, handleCancel, handleChangeVolume
        }
    }
}
</script>
<style lang="stylus" scoped>
.volume {
    font-size: 2rem;
    padding-left: 1rem;

    .value {
        font-size: 1.4rem;
    }
}

.slider-wrap {
    text-align: center;

    // height: 10rem;
    .silder {
        padding: 2rem 4rem;
    }

    .btn {
        padding: 2rem 0;
    }

    .btn+.btn {
        padding-top: 0rem;
    }
}
</style>