<template>
    <el-dialog class="options" title="调节音量" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" :data="globalData.devices" :items="state.items" label="MachineName" title="全选">
                    <template #title>
                        <div>
                            <el-button size="small" @click="handleSelectMute">状态选择</el-button>
                        </div>
                    </template>
                    <template #name="scope">
                        <span>
                            <span class="name">
                                {{scope.item.MachineName}}
                            </span>
                            <strong class="volume">
                                <template v-if="scope.item.VolumeMute">
                                    <el-icon>
                                        <Mute />
                                    </el-icon>
                                </template>
                                <template v-else>
                                    <el-icon>
                                        <Microphone />
                                    </el-icon>
                                </template>
                                <strong class="value">{{scope.item.Volume.Value?Math.floor(scope.item.Volume.Value):scope.item.Volume.Value}}%</strong>
                            </strong>
                        </span>
                    </template>
                </CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="commands">
                <PrevBoxWrap ref="commands" title="调节音量">
                    <template #wrap>
                        <div class="slider-wrap flex flex-column">
                            <div class="silder flex flex-1">
                                <div class="flex-1">
                                    <el-slider @change="handleChangeVolume" v-model="state.volume" vertical height="100%" />
                                </div>
                            </div>
                            <div class="btn">
                                <el-button @click="handleMute(true)">设置静音</el-button>
                            </div>
                            <div class="btn">
                                <el-button @click="handleMute(false)">取消静音</el-button>
                            </div>
                        </div>
                    </template>
                </PrevBoxWrap>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain @click="handleCancel">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import PrevBoxWrap from '../../boxs/PrevBoxWrap.vue'
import { ElMessage } from 'element-plus';
import { setVolumeMute, setVolume } from '../../../../apis/volume'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.volume.items),
            mute: false,
            loading: false,
            volume: 0
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

        const handleSelectMute = () => {
            state.items = globalData.value.devices.filter(c => c.VolumeMute == state.mute);
            ElMessage.success(`已选中${state.mute ? '静音' : '未静音'}设备`)
            state.mute = !state.mute;
        }

        const items = ref(null);
        const handleMute = (mute) => {
            let _items = items.value.getData();
            if (_items.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }
            setVolumeMute(_items, mute);
        }
        const handleChangeVolume = () => {
            let _items = items.value.getData();
            if (_items.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }
            setVolume(_items, state.volume / 100);
        }


        return {
            state, globalData, items, handleCancel, handleSelectMute, handleMute, handleChangeVolume
        }
    }
}
</script>
<style lang="stylus" scoped>
.command-wrap {
    height: 60vh;

    .items {
        height: 100%;
        width: 60%;
        position: relative;
    }

    .commands {
        height: 100%;
        width: 38%;
        position: relative;
    }

    .volume {
        font-size: 2rem;
        padding-left: 1rem;

        .value {
            font-size: 1.4rem;
        }
    }

    .slider-wrap {
        height: 100%;
        text-align: center;

        .silder {
            padding: 2rem 0;
        }

        .btn {
            padding: 2rem 0;
        }

        .btn+.btn {
            padding-top: 0rem;
        }
    }
}
</style>