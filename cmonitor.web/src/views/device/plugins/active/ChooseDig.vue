<template>
    <el-dialog class="options" title="阻止窗口" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="rule-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="Exes flex flex-column">
                <div class="private">
                    <CheckBoxWrap ref="privateExes" :data="state.privateExes" :items="state.currentPrivate" label="ID" text="Name" title="私有窗口">
                    </CheckBoxWrap>
                </div>
                <div class="flex-1"></div>
                <div class="public">
                    <CheckBoxWrap ref="publicExes" :data="state.publicExes" :items="state.currentPublic" label="ID" text="Name" title="公共窗口">
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
import { computed, onMounted, watch } from '@vue/runtime-core';
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
            items: computed(() => pluginState.value.activeWindow.devices),
            privateExes: computed(() => user.value ? user.value.Windows : []),
            publicExes: computed(() => usePublic ? publicUser.value.Windows : []),
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

        onMounted(() => {
            if (state.items.length == 1) {
                let item = state.items[0];
                state.currentPrivate = state.privateExes.filter(c => item.DisallowRunIds.indexOf(c.ID) >= 0);
                state.currentPublic = state.publicExes.filter(c => item.DisallowRunIds.indexOf(c.ID) >= 0);
            }
        });

        const devices = ref(null);
        const privateExes = ref(null);
        const publicExes = ref(null);
        const parseRule = () => {
            const _privateIds = privateExes.value.getData().map(c => c.ID);
            const _privateExes = state.privateExes.filter(c => _privateIds.indexOf(c.ID) >= 0);
            const _publicIds = publicExes.value.getData().map(c => c.ID);
            const _publicExes = state.publicExes.filter(c => _publicIds.indexOf(c.ID) >= 0);
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
                ids: _privateIds.concat(_publicIds),
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
                activeDisallow(_devices.map(c => c.MachineName), exes.exes, exes.ids).then((res) => {
                    state.loading = false;
                    ElMessage.success('操作成功！');
                    globalData.value.devices.filter(c => _devices.indexOf(c.MachineName) >= 0).forEach(device => {
                        device.DisallowRunIds = exes.ids;
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