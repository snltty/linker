<template>
    <el-dialog class="options" title="阻止窗口" destroy-on-close v-model="state.show" center align-center width="94%">
        <el-alert style="margin-bottom:.6rem" type="error" title="无法上网与窗口无关" :closable="false" show-icon />
        <div class="rule-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="Exes flex flex-column">
                <div class="private">
                    <CheckBoxWrap ref="privateExes" :data="state.privateExes" :items="state.currentPrivate" label="Name" text="Name" title="私有窗口">
                    </CheckBoxWrap>
                </div>
                <div class="flex-1"></div>
                <div class="public">
                    <CheckBoxWrap ref="publicExes" :data="state.publicExes" :items="state.currentPublic" label="Name" text="Name" title="公共窗口">
                    </CheckBoxWrap>
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
import { ElMessage, ElMessageBox } from 'element-plus';
import { activeDisallow } from '@/apis/active'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const user = computed(() => globalData.value.usernames[globalData.value.username]);
        const publicUserName = globalData.value.publicUserName;
        const publicUser = computed(() => globalData.value.usernames[publicUserName]);
        const usePublic = publicUser.value && globalData.value.username != publicUserName;

        const state = reactive({
            show: props.modelValue,
            items: computed(() => {
                const devices = pluginState.value.activeWindow.devices;
                let ids1 = devices.reduce((arr, value) => {
                    arr.push(...value.ActiveWindow.DisallowRunIds1);
                    return arr;
                }, []);
                let ids2 = devices.reduce((arr, value) => {
                    arr.push(...value.ActiveWindow.DisallowRunIds2);
                    return arr;
                }, []);
                state.currentPrivate = state.privateExes.filter(c => ids1.indexOf(c.Name) >= 0);
                state.currentPublic = state.publicExes.filter(c => ids2.indexOf(c.Name) >= 0);
                return devices;
            }),
            privateExes: computed(() => user.value ? user.value.Windows || [] : []),
            publicExes: computed(() => usePublic ? publicUser.value.Windows || [] : []),
            loading: false,
            currentPrivate: [],
            currentPublic: [],
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const devices = ref(null);
        const privateExes = ref(null);
        const publicExes = ref(null);
        const parseRule = () => {
            const _privateIds = privateExes.value.getData().map(c => c.Name);
            const _privateExes = state.privateExes.filter(c => _privateIds.indexOf(c.Name) >= 0);
            const _publicIds = publicExes.value.getData().map(c => c.Name);
            const _publicExes = state.publicExes.filter(c => _publicIds.indexOf(c.Name) >= 0);
            const exes = _privateExes.concat(_publicExes).reduce((data, item, index) => {
                let arr = item.List.reduce((val, item, index) => {
                    val = val.concat(item.Name.split(','));
                    return val;
                }, []);
                for (let i = 0; i < arr.length; i++) {
                    if (arr[i] && data.indexOf(arr[i]) == -1) {
                        data.push(arr[i]);
                    }
                }
                return data;
            }, []);

            return {
                ids1: _privateIds,
                ids2: _publicIds,
                exes: exes
            };
        }
        const handleSubmit = () => {
            const _devices = devices.value.getData();
            if (_devices.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }

            ElMessageBox.confirm('如果未选择程序，则视为清空程序，是否确定应用？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                state.loading = true;
                const exes = parseRule();
                activeDisallow(_devices.map(c => c.MachineName), exes.exes, exes.ids1, exes.ids2).then((res) => {
                    state.loading = false;
                    ElMessage.success('操作成功！');
                    globalData.value.devices.filter(c => _devices.indexOf(c.MachineName) >= 0).forEach(device => {
                        device.ActiveWindow.DisallowRunIds1 = exes.ids1;
                        device.ActiveWindow.DisallowRunIds2 = exes.ids2;
                    });
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error('操作失败');
                });

            }).catch((e) => {
                console.log(e);
            });
        }
        const handleCancel = () => {
            state.show = false;
        }


        return {
            state, globalData, devices, privateExes, publicExes, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.rule-wrap {
    height: 70vh;

    .items {
        height: 100%;
        width: 48%;
        position: relative;
    }

    .Exes {
        height: 100%;
        width: 48%;
        position: relative;

        .private, .public {
            height: 49%;
            position: relative;
        }
    }
}
</style>