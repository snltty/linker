<template>
    <el-dialog class="options options-center" title="模式" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <div class="modes-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="modes">
                <div class="inner absolute scrollbar">
                    <template v-for="(item,index) in state.modes" :key="index">
                        <div class="item">
                            <div class="subitem flex">
                                <span class="label flex-1">{{item.Name}}</span>
                                <el-button @click="handleUseMode(item)">应用-{{item.Name}}</el-button>
                            </div>
                        </div>
                    </template>
                </div>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, onMounted, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'

import { ElMessage } from 'element-plus';
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
import { useModes } from '../../../../apis/modes';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const devices = ref(null);
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.modes.devices),
            modes: computed(() => (globalData.value.usernames[globalData.value.username] || {}).Modes || []),
            loading: false
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
        const handleUseMode = (item) => {
            const devicesNames = devices.value.getData().map(c => c.MachineName);
            if (devicesNames.length == 0) {
                ElMessage.error('请选择设备');
                return;
            }
            const funcs = [];
            const json = JSON.parse(item.Data);
            for (let j in json) {
                if (json[j].use) {
                    funcs.push(useModes(devicesNames, json[j].list, json[j].ids1, json[j].ids2, json[j].path));
                }
            }
            Promise.all(funcs).then(res => {
                ElMessage.success('设置成功');
            }).catch(() => {
                ElMessage.error('设置失败');
            });
        }

        return {
            state, globalData, devices, handleCancel, handleUseMode
        }
    }
}
</script>
<style lang="stylus" scoped>
.modes-wrap {
    height: 70vh;
    position: relative;

    .items {
        height: 100%;
        width: 36%;
        position: relative;
    }

    .modes {
        height: 100%;
        width: 62%;
        position: relative;
        border: 1px solid #ddd;
        box-sizing: border-box;

        .label {
            line-height: 3.2rem;
        }

        .inner {
            padding: 0.6rem;

            &>div {
                padding: 0.6rem 0;
            }
        }
    }
}
</style>