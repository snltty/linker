<template>
    <el-dialog class="options" title="网络限制" destroy-on-close v-model="state.show" center align-center width="94%">
        <el-alert style="margin-bottom:.6rem" type="error" title="打开不了软件与网络无关" :closable="false" show-icon />
        <div class="rule-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="rules flex flex-column">
                <div class="private">
                    <CheckBoxWrap ref="privateRules" :data="state.privateRules" :items="state.currentPrivate" label="ID" text="Name" title="私有限制"></CheckBoxWrap>
                </div>
                <div class="flex-1"></div>
                <div class="public">
                    <CheckBoxWrap ref="publicRules" :data="state.publicRules" :items="state.currentPublic" label="ID" text="Name" title="公共限制"></CheckBoxWrap>
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
import { computed, inject, onMounted, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { setRules } from '../../../../apis/hijack'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    props: ['modelValue', 'items'],
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
                const devices = pluginState.value.hijack.devices;
                let ids = devices.reduce((arr, value) => {
                    arr.push(...value.Hijack.RuleIds);
                    return arr;
                }, []);
                state.currentPrivate = state.privateRules.filter(c => ids.indexOf(c.ID) >= 0);
                state.currentPublic = state.publicRules.filter(c => ids.indexOf(c.ID) >= 0);
                return devices;
            }),
            privateRules: computed(() => user.value ? user.value.Rules : []),
            publicRules: computed(() => usePublic ? publicUser.value.Rules : []),
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
        const privateRules = ref(null);
        const publicRules = ref(null);
        const parseRule = () => {
            const _privateRules = privateRules.value.getData().map(c => c.ID);
            const _publicRules = publicRules.value.getData().map(c => c.ID);
            const _user = user.value;
            const _publicUser = publicUser.value;

            const publicList = _user.Rules.filter(c => _privateRules.indexOf(c.ID) >= 0).map(rule => {
                return _user.Processs.filter(c => rule.PrivateProcesss.indexOf(c.ID) >= 0);
            });
            const privateList = _publicUser.Rules.filter(c => _publicRules.indexOf(c.ID) >= 0).map(rule => {
                return _publicUser.Processs.filter(c => rule.PublicProcesss.indexOf(c.ID) >= 0);
            });

            const origin = publicList.concat(privateList).reduce((arr, value, index) => {
                arr = arr.concat(value.reduce((arr, value, index) => {
                    arr = arr.concat(value.List);
                    return arr;
                }, []));
                return arr;
            }, []);
            const res = [];
            origin.forEach(element => {
                if (res.filter(c => c.Name == element.Name && c.DataType == element.DataType && c.AllowType == element.AllowType).length == 0) {
                    res.push(element);
                }
            });

            return {
                ids: _privateRules.concat(_publicRules),
                list: {
                    AllowProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 0).map(c => c.Name),
                    DeniedProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 1).map(c => c.Name),
                    AllowDomains: res.filter(c => c.DataType == 1 && c.AllowType == 0).map(c => c.Name),
                    DeniedDomains: res.filter(c => c.DataType == 1 && c.AllowType == 1).map(c => c.Name),
                    AllowIPs: res.filter(c => c.DataType == 2 && c.AllowType == 0).map(c => c.Name),
                    DeniedIPs: res.filter(c => c.DataType == 2 && c.AllowType == 1).map(c => c.Name),
                }
            }
        }
        const handleSubmit = () => {
            const _devices = devices.value.getData();
            if (_devices.length == 0) {
                ElMessage.error('未选择任何设备');
                return;
            }

            ElMessageBox.confirm('如果未选择任何限制，则视为清空限制，是否确定应用限制？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {

                state.loading = true;
                const rules = parseRule();
                setRules({
                    Devices: _devices.map(c => c.MachineName),
                    Rules: rules.list,
                    ids: rules.ids
                }).then((errorDevices) => {
                    state.loading = false;
                    if (errorDevices && errorDevices.length > 0) {
                        ElMessage.error(`操作失败，失败设备:${errorDevices.join(',')}`);
                    } else {
                        ElMessage.success('操作成功！');
                        globalData.value.devices.filter(c => _devices.indexOf(c.MachineName) >= 0).forEach(device => {
                            device.Hijack.RuleIds = rules.ids;
                        });
                    }
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error('操作失败');
                });

            }).catch(() => { });
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            state, globalData, devices, privateRules, publicRules, handleSubmit, handleCancel
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

    .rules {
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