<template>
    <el-dialog class="options" title="发送提醒" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="prevs-wrap flex flex-column flex-nowrap">
                <div class="prevs">
                    <PrevBoxWrap ref="prevs" :data="state.prevs" @prev="handlePrev" title="快捷短语"></PrevBoxWrap>
                </div>
                <div class="flex-1"></div>
                <div>
                    <div class="times">
                        <el-input v-model="state.sec" size="large">
                            <template #append>秒钟/次</template>
                        </el-input>
                    </div>
                    <div class="prev">
                        <el-input v-model="state.prev" type="textarea" resize="none" placeholder="输入提醒消息"></el-input>
                    </div>
                </div>
                <div class="record flex">
                    <div class="text">
                        <span v-if="state.disabled">
                            <font color="red">此环境不支持录音</font>
                        </span>
                        <span v-else-if="state.recoeding">正在录音</span>
                        <span v-else-if="state.recoedData" @click="handleClearRecord">已录音({{state.duration}}s)</span>
                        <span v-else>未录音</span>
                    </div>
                    <span class="flex-1"></span>
                    <el-button plain :disabled="state.disabled" @touchstart="handleStartRecord" @touchend="handleEndRecord">录音</el-button>
                </div>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import PrevBoxWrap from '../../boxs/PrevBoxWrap.vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '../../../../apis/command'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
import { play } from '@/apis/volume'
import pako from 'pako';

export default {
    pluginName:'message',
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.message.devices),
            prevs: [
                '请注意上课纪律!',
                '请勿玩游戏!',
                '请勿大声喧哗!'
            ],
            sec: 10,
            prev: '',
            loading: false,
            recoeding: false,
            recoedData: null,
            disabled: false,
            duration: 0
        });
        try {
            if (pluginState.value.message.devices.length == 1 && pluginState.value.message.devices[0].Share.UserName.Value) {
                state.prevs.push(`【${pluginState.value.message.devices[0].Share.UserName.Value}】请注意上课纪律!`);
            }
        } catch (e) {
        }
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handlePrev = (item) => {
            state.prev = item;
        }

        const devices = ref(null);
        const prevs = ref(null);
        const handleSubmit = () => {
            let _devices = devices.value.getData();
            if (_devices.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }
            if (state.prev.length == 0 && !state.recoedData) {
                ElMessage.error('未填写消息');
                return;
            }

            ElMessageBox.confirm('是否确定发送消息？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                state.loading = true;
                const names = _devices.map(c => c.MachineName);
                const fn = state.recoedData ? play(names, state.recoedData) : exec(names, [`start cmonitor.message.win.exe "${state.prev}" ${state.sec}`]);
                fn.then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                        handleClearRecord();
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

        let recorder = new MP3Recorder({
            debug: false,
            funOk: () => { },
            funCancel: (msg) => {
                state.disabled = true;
                ElMessage.error(msg);
                //recorder = null;
            }
        });
        const handleClearRecord = () => {
            state.recoedData = null;
            state.duration = 0;
        }
        const handleStartRecord = () => {
            if (recorder && recorder.start) {
                state.loading = true;
                state.recoeding = true;
                recorder.start();
            }

        }
        const handleEndRecord = () => {
            state.loading = false;
            state.recoeding = false;
            if (recorder && recorder.stop) {
                recorder.stop();
                recorder.getMp3Blob((e, blob) => {
                    blob.arrayBuffer().then((arrayBuffer) => {
                        const array = new Uint8Array(arrayBuffer);
                        if (array.length > 0) {
                            const compressedData = pako.gzip(array);
                            state.recoedData = btoa(String.fromCharCode(...new Uint8Array(compressedData)));
                        }
                    });
                    const audioElement = new Audio(URL.createObjectURL(blob));
                    audioElement.addEventListener('loadedmetadata', () => {
                        state.duration = parseInt(audioElement.duration);
                    });
                });
            }

        }

        return {
            state, globalData, devices, prevs, handleSubmit, handleCancel, handlePrev, handleStartRecord, handleEndRecord, handleClearRecord
        }
    }
}
</script>
<style lang="stylus" scoped>
.command-wrap {
    height: 70vh;

    .items {
        height: 100%;
        width: 36%;
        position: relative;
    }

    .prevs-wrap {
        height: 100%;
        width: 62%;
        position: relative;

        .times {
            margin: 0.6rem 0;
        }

        .record {
            padding-top: 0.6rem;

            .text {
                line-height: 3.2rem;
            }
        }

        .prevs {
            height: 100%;
            width: 100%;
            position: relative;
        }
    }
}
</style>