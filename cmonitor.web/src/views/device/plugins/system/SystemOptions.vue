<template>
    <el-dialog class="options" title="设备选项" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap common-command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" @change="handleDevicesChange" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="commands">
                <div class="inner absolute scrollbar">
                    <ul>
                        <template v-for="(item,index) in state.options" :key="index">
                            <li class="flex">
                                <span class="label">{{item.label}}</span>
                                <span class="flex-1"></span>
                                <div class="options">
                                    <template v-if="pluginState.system.devices.length == 1">
                                        <el-switch @change="handleOptionChange(item)" v-model="item.value" inline-prompt active-text="禁用" inactive-text="启用" size="large" />
                                    </template>
                                    <template v-else>
                                        <el-button size="default" type="danger" plain @click="handleSubmit(item,true)">禁用</el-button>
                                        <el-button size="default" type="success" plain @click="handleSubmit(item,false)">开启</el-button>
                                    </template>
                                </div>
                            </li>
                        </template>
                    </ul>
                </div>
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
import { injectPluginState } from '../../provide';
import { injectGlobalData } from '@/views/provide';
import { updateRegistryOptions } from '@/apis/system';
import { ElMessage } from 'element-plus';
export default {
    pluginName:'cmonitor.plugin.system.',
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();

        const state = reactive({
            show: props.modelValue,
            items:pluginState.value.system.devices,
            loading: false,
            options: []
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const items = ref(null);
        const handleCancel = () => {
            state.show = false;
        }

        const initOptions = () => {
            const devices = pluginState.value.system.devices.map(c => c.MachineName);
            const optionJson = globalData.value.allDevices.filter(c => devices.indexOf(c.MachineName) >= 0).reduce((json, value, index) => {
                json = Object.assign(json, value.System.OptionKeys);
                return json;
            }, {});
            const keys = Object.keys(optionJson);
            const arr = keys.map(c => {
                const item = optionJson[c];
                return { key: c, label: item.Desc, index: item.Index, value: false }
            }).filter(c => c.label).sort((a, b) => a.index - b.index);
            state.options = arr;
        }
        const handleDevicesChange = (devices) => {
            pluginState.value.system.devices = devices;
            initOptions();

            if (pluginState.value.system.devices.length == 1) {
                const name = pluginState.value.system.devices[0].MachineName;
                const device = globalData.value.allDevices.filter(c => c.MachineName == name)[0];
                if (device) {

                    const registrys = device.System.OptionValues;
                    const options = state.options;
                    if (registrys && registrys.length == options.length) {
                        for (let i = 0; i < registrys.length; i++) {
                            options[i].value = registrys[i] == 1;
                        }
                        state.options = options;
                    }

                }
            }
        }

        const handleOptionChange = (item) => {
            updateRegistryOptions(pluginState.value.system.devices.map(c => c.MachineName), [{ key: item.key, value: item.value }]).then(() => {
                ElMessage.success('已执行');
            }).catch(() => {
                ElMessage.error('执行失败');
            })
        }

        const handleSubmit = (item, value) => {
            if (pluginState.value.system.devices.length == 0) {
                ElMessage.error('请选择一个设备');
                return;
            }
            updateRegistryOptions(pluginState.value.system.devices.map(c => c.MachineName), [{ key: item.key, value: value }]).then(() => {
                ElMessage.success('已执行');
            }).catch(() => {
                ElMessage.error('执行失败');
            });
        }

        return {
            state, globalData, pluginState, items, handleCancel, handleDevicesChange, handleOptionChange, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch {
    --el-switch-on-color: rgba(255, 0, 0, 0.8) !important;
}

.el-button+.el-button {
    margin-left: 0.6rem;
}

.el-button {
    padding: 6px 12px;
}

.command-wrap {
    height: 70vh;

    .items {
        height: 100%;
        width: 32%;
        position: relative;
    }

    .commands {
        height: 100%;
        width: 67%;
        position: relative;
        border: 1px solid #ddd;
        box-sizing: border-box;

        .inner {
            padding: 0.6rem;

            .options {
                line-height: 4rem;
            }

            span.label {
                line-height: 4rem;
            }
        }
    }
}
</style>