<template>
    <el-dialog class="options" title="发送提醒" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="command-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="items" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
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
                            <template #append>秒钟</template>
                        </el-input>
                    </div>
                    <div class="prev">
                        <el-input v-model="state.prev" type="textarea" resize="none" placeholder="输入提醒消息"></el-input>
                    </div>
                </div>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="state.loading" @click="handleSubmit">确 定</el-button>
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
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap, PrevBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            items: computed(() => pluginState.value.message.items),
            prevs: [
                '请注意上课纪律!',
                '请勿玩游戏!',
                '请勿大声喧哗!'
            ],
            sec: 10,
            prev: '',
            loading: false
        });
        try {
            if (pluginState.value.message.items.length == 1 && pluginState.value.message.items[0].Share.UserName) {
                state.prevs.push(`【${pluginState.value.message.items[0].Share.UserName}】请注意上课纪律!`);
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

        const items = ref(null);
        const prevs = ref(null);
        const handleSubmit = () => {
            let _items = items.value.getData();
            if (_items.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }
            if (state.prev.length == 0) {
                ElMessage.error('未填写消息');
                return;
            }

            ElMessageBox.confirm('是否确定发送消息？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                state.loading = true;
                exec(_items, [`start message.win.exe "${state.prev}" ${state.sec}`]).then((res) => {
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
            state, globalData, items, prevs, handleSubmit, handleCancel, handlePrev
        }
    }
}
</script>
<style lang="stylus" scoped>
.command-wrap {
    height: 60vh;

    .items {
        height: 100%;
        width: 48%;
        position: relative;
    }

    .prevs-wrap {
        height: 100%;
        width: 48%;
        position: relative;

        .times {
            margin: 0.6rem 0;
        }

        .prevs {
            height: 100%;
            width: 100%;
            position: relative;
        }
    }
}
</style>