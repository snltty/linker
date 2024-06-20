<template>
    <el-dialog class="options" title="命令终端" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="term-wrap flex relative">
            <div id="terminal" class="absolute"></div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, nextTick, onMounted, onUnmounted, watch } from '@vue/runtime-core';
import { Terminal } from 'xterm';
import { FitAddon } from '@xterm/addon-fit';
import { commandAlive, commandStart, commandStop, commandWrite } from '@/apis/command'

import 'xterm/css/xterm.css'
import { injectPluginState } from '../../provide';
import { ElMessage } from 'element-plus';
import { subNotifyMsg, unsubNotifyMsg } from '@/apis/request';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const terminal = new Terminal();
        const fitAddon = new FitAddon();
        terminal.loadAddon(fitAddon);
        terminal.promptStr = '$ ';
        terminal.prompt = () => {
            terminal.write(terminal.promptStr);
        }
        terminal.init = () => {
            terminal.open(document.getElementById('terminal'));
            fitAddon.fit();
            terminal.prompt();

            let command = '';
            terminal.onKey((ev) => {
                if (ev.domEvent.keyCode === 13) {
                    terminal.write('\r\n');
                    terminal.command(command);
                    command = '';
                } else if (ev.domEvent.keyCode === 8) {
                    if (terminal._core.buffer.x > terminal.promptStr.length) {
                        terminal.write('\b \b');
                    }
                } else {
                    terminal.write(ev.key);
                    command += ev.key;
                }
            });

        }
        terminal.commands = {
            'cls': () => {
                terminal.clear();
            },
            'clear': () => {
                terminal.clear();
            }
        }
        terminal.command = (command) => {
            if (terminal.commands[command]) {
                terminal.commands[command]();
                terminal.prompt();
                return;
            }
            commandWrite(state.name, state.commandId, `${command}\r\n`);
        }
        terminal.onRemoteData = (res) => {
            if (res.Id == state.commandId) {
                terminal.writeln(res.Data);
                if (res.Data == '') {
                    terminal.prompt();
                }

            }
        }

        const state = reactive({
            show: props.modelValue,
            loading: false,
            commandId: 0,
            name: ''
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


        let commandAliveTimer = 0;
        const getCommandId = () => {
            if (pluginState.value.command.devices.length > 0) {
                state.name = pluginState.value.command.devices[0].MachineName;
                commandStart(state.name).then((res) => {
                    state.commandId = res;
                    if (state.commandId == 0) {
                        ElMessage.error('启动失败~');
                        state.show = false;
                    } else {
                        const fn = () => {
                            commandAlive(state.name, state.commandId).then(() => {
                                commandAliveTimer = setTimeout(fn, 1000);
                            }).catch(() => {
                                commandAliveTimer = setTimeout(fn, 1000);
                            });
                        }
                        fn();

                        subNotifyMsg('/notify/command/data', terminal.onRemoteData);
                    }
                });
            } else {
                state.show = false;
            }
        }
        onMounted(() => {
            nextTick(() => {
                terminal.init();
                getCommandId();
            });
        });
        onUnmounted(() => {
            clearTimeout(commandAliveTimer);
            unsubNotifyMsg('/notify/command/data', terminal.onRemoteData);
            commandStop(state.name, state.commandId);
        });

        return {
            state, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.term-wrap {
    height: 70vh;
}
</style>