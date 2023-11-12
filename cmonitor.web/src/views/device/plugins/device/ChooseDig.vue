<template>
    <el-dialog class="options" title="选择你的设备" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <div class="devices-wrap">
            <CheckBoxWrap ref="devices" :data="state.list" :items="state.items" label="MachineName" text="MachineName" title="选择设备">
                <template #oper="scope">
                    <div>
                        <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDel(scope.item.MachineName)">
                            <template #reference>
                                <span class="del-btn">
                                    <el-icon>
                                        <Delete />
                                    </el-icon>
                                </span>
                            </template>
                        </el-popconfirm>
                    </div>
                </template>
            </CheckBoxWrap>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, onMounted, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import { updateDevices } from '../../../../apis/hijack'
import { delDevice } from '../../../../apis/signin'
import { ElMessage } from 'element-plus';
import { injectGlobalData } from '@/views/provide';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();;
        const state = reactive({
            show: props.modelValue,
            loading: false,
            list: computed(() => globalData.value.allDevices),
            items: computed(() => globalData.value.devices),
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
            globalData.value.updateFlag = Date.now();
        }
        const handleDel = (name) => {
            state.loading = true;
            delDevice(name).then(() => {
                state.loading = false;
                globalData.value.updateFlag = Date.now();
            }).catch(() => {
                state.loading = false;
            });
        }

        const devices = ref(null);
        const handleSubmit = () => {
            const _devices = devices.value.getData();

            state.loading = true;
            updateDevices({
                username: globalData.value.username,
                devices: _devices.map(c => c.MachineName)
            }).then((error) => {
                state.loading = false;
                globalData.value.updateFlag = Date.now();
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功！');
                    state.show = false;
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败!');
            });
        }

        onMounted(() => {
            globalData.value.updateFlag = Date.now();
        });

        return {
            state, devices, handleCancel, handleSubmit, handleDel
        }
    }
}
</script>
<style lang="stylus" scoped>
.devices-wrap {
    height: 70vh;
    position: relative;

    .del-btn {
        font-size: 2rem;
    }
}
</style>