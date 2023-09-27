<template>
    <el-dialog class="options" title="执行命令" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap common-command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="commands">
                <PrevBoxWrap ref="commands" :data="state.commands" title="命令多发">
                    <template #default="scope">
                        <div class="btn">
                            <el-button :loading="state.loading" @click="handleCommand(scope.item)">
                                {{scope.item.label}}
                            </el-button>
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
import { computed, inject, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import PrevBoxWrap from '../../boxs/PrevBoxWrap.vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '../../../../apis/command'
import { llockUpdate } from '../../../../apis/llock'
import { wallpaperUpdate } from '../../../../apis/wallpaper'
import { usbUpdate } from '../../../../apis/usb'
import { setVolumeMute } from '../../../../apis/volume'
import { injectPluginState } from '../../provide';
import { injectGlobalData } from '@/views/provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();

        const wallpaperFunc = (names, value) => {
            wallpaperUpdate(names, value, `http://${window.location.hostname}:${window.location.port}/bg.jpg`);
        }
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.command.items),
            commands: [
                { label: '强制关机', value: 'shutdown -s -f -t 00' },
                { label: '强制重启', value: 'shutdown -r -f -t 00' },
                { label: '打开锁屏', func: llockUpdate, value: true },
                { label: '关闭锁屏', func: llockUpdate, value: false },
                { label: '开资源管理器', value: 'start explorer.exe' },
                { label: '关资源管理器', value: 'taskkill /f /t /im "explorer.exe"' },
                { label: '打开壁纸', func: wallpaperFunc, value: true },
                { label: '关闭壁纸', func: wallpaperFunc, value: false },
                { label: '禁用U盘', func: usbUpdate, value: true },
                { label: '启用U盘', func: usbUpdate, value: false },
                { label: '设置静音', func: setVolumeMute, value: true },
                { label: '取消静音', func: setVolumeMute, value: false },
            ],
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
        const handleCommand = (commandItem) => {
            let _items = items.value.getData();
            if (_items.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }

            ElMessageBox.confirm('是否确定执行命令？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                state.loading = true;
                const func = commandItem.func ? commandItem.func(_items, commandItem.value) : exec(_items, [commandItem.value]);
                func.then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败');
                    }
                    state.loading = false;
                }).catch(() => {
                    state.loading = false;
                    ElMessage.error('操作失败');
                });

            }).catch(() => { });
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            state, globalData, items, handleCancel, handleCommand
        }
    }
}
</script>
<style lang="stylus">
.common-command-wrap {
    .checkbox-wrap {
        li:nth-child(2n) {
            padding-bottom: 1rem;
        }
    }
}
</style>
<style lang="stylus" scoped>
.command-wrap {
    height: 60vh;

    .items {
        height: 100%;
        width: 48%;
        position: relative;
    }

    .commands {
        height: 100%;
        width: 48%;
        position: relative;
    }

    .btn {
        text-align: center;
        padding: 0.2rem 0;
        width: 100%;
    }
}
</style>