<template>
    <el-dialog class="options" title="执行命令" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap common-command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" @change="handleDevicesChange" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="commands">
                <div class="inner absolute scrollbar">
                    <template v-for="(item,index) in commandModules" :key="index">
                        <component :is="item"></component>
                    </template>
                </div>
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
import { injectPluginState } from '../../provide';
import { injectGlobalData } from '@/views/provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const commandFiles = require.context('../../plugins/', true, /Command\.vue/);
        const commandModules = commandFiles.keys().map(c => commandFiles(c).default);

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();

        const state = reactive({
            show: props.modelValue,
            items:pluginState.value.command.devices,
            loading: false
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
        const handleDevicesChange = (devices) => {
            pluginState.value.command.devices = devices;
        }

        return {
            state, globalData, items, commandModules, handleCancel, handleDevicesChange
        }
    }
}
</script>
<style lang="stylus">
.command-wrap {
    .commands {
        .item {
            padding: 0.6rem;
            font-size: 1.2rem;

            .subitem {
                padding: 0.2rem;

                .label {
                    margin-right: 0.6rem;
                }

                .el-button+.el-button {
                    margin-left: 0.6rem;
                }
            }
        }

        .inner {
            padding: 0.6rem;
        }
    }
}
</style>
<style lang="stylus" scoped>
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

            &>div {
                padding: 0.6rem 0;
            }

            .item {
                padding: 0.6rem;

                .subitem {
                    padding: 0.2rem;

                    .label {
                        margin-right: 0.6rem;
                    }
                }
            }
        }
    }

    .btn {
        text-align: center;
        padding: 0.2rem 0;
        width: 100%;
    }
}
</style>