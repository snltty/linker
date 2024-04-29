<template>
    <el-dialog class="options" title="调节亮度" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" :data="globalData.devices" :items="state.items" label="MachineName" title="全选">
                    <template #name="scope">
                        <span>
                            <span class="name">
                                {{scope.item.MachineName}}
                            </span>
                            <strong class="light">
                                <el-icon>
                                    <Sunny />
                                </el-icon>
                                <strong class="value">{{scope.item.Light.Value?Math.floor(scope.item.Light.Value):scope.item.Light.Value}}%</strong>
                            </strong>
                        </span>
                    </template>
                </CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="commands">
                <PrevBoxWrap ref="commands" title="调节亮度">
                    <template #wrap>
                        <div class="slider-wrap flex flex-column">
                            <div class="silder flex flex-1">
                                <div class="flex-1">
                                    <el-slider type="success" @change="handleChangeLight" v-model="state.light" vertical height="100%" />
                                </div>
                            </div>
                        </div>
                    </template>
                </PrevBoxWrap>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <!-- <el-button type="success" plain @click="handleCancel">确 定</el-button> -->
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import PrevBoxWrap from '../../boxs/PrevBoxWrap.vue'
import { ElMessage } from 'element-plus';
import { setLight } from '../../../../apis/light'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'cmonitor.plugin.light.',
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.light.devices),
            mute: false,
            loading: false,
            light: 0
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

        const devices = ref(null);
        const handleChangeLight = () => {
            let _devices = devices.value.getData();
            if (_devices.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }
            setLight(_devices.map(c => c.MachineName), state.light);
        }


        return {
            state, globalData, devices, handleCancel, handleChangeLight
        }
    }
}
</script>
<style lang="stylus" scoped>
.command-wrap {
    height: 70vh;

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

    .light {
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