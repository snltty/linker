<template>
    <el-dialog class="options" title="网络限制" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="flex">
            <div><el-alert style="margin-bottom:.6rem" type="error" title="打开不了软件与网络无关" :closable="false" show-icon /></div>
            <div class="flex-1"></div>
            <div>
                <el-checkbox v-model="state.domainKill">暴力强杀</el-checkbox>
            </div>
        </div>
        <div class="rule-wrap flex">
            <div class="items">
                <CheckBoxWrap ref="devices" @change="handleDevicesChange" :data="globalData.devices" :items="state.items" label="MachineName" title="选择设备"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="rules flex flex-column">
                <div class="private">
                    <CheckBoxWrap ref="privateRules" :data="state.privateRules" :items="state.currentPrivate" label="Name" text="Name" title="私有限制"></CheckBoxWrap>
                </div>
                <div class="flex-1"></div>
                <div class="public">
                    <CheckBoxWrap ref="publicRules" :data="state.publicRules" :items="state.currentPublic" label="Name" text="Name" title="公共限制"></CheckBoxWrap>
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
import { setRules } from '../../../../apis/hijack'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'cmonitor.plugin.hijack.',
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
            items:[],
            privateRules: computed(() => user.value ? user.value.Processs || [] : []),
            publicRules: computed(() => usePublic ? publicUser.value.Processs || [] : []),
            loading: false,
            currentPrivate: [],
            currentPublic: [],
            domainKill: false
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
            const _privateRules = privateRules.value.getData().map(c => c.Name);
            const _publicRules = publicRules.value.getData().map(c => c.Name);
            const _user = user.value;
            const _publicUser = publicUser.value;

            const publicList = _user.Processs.filter(c => _privateRules.indexOf(c.Name) >= 0);
            const privateList = _publicUser.Processs.filter(c => _publicRules.indexOf(c.Name) >= 0);

            const origin = publicList.concat(privateList).reduce((arr, value, index) => {
                arr = arr.concat(value.List);
                return arr;
            }, []);
            const res = [];
            origin.forEach(element => {
                if (res.filter(c => c.Name == element.Name && c.DataType == element.DataType && c.AllowType == element.AllowType).length == 0) {
                    res.push(element);
                }
            });

            return {
                ids1: _privateRules,
                ids2: _publicRules,
                list: {
                    AllowProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 0).map(c => c.Name),
                    DeniedProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 1).map(c => c.Name),
                    AllowDomains: res.filter(c => c.DataType == 1 && c.AllowType == 0).map(c => c.Name),
                    DeniedDomains: res.filter(c => c.DataType == 1 && c.AllowType == 1).map(c => c.Name),
                    AllowIPs: res.filter(c => c.DataType == 2 && c.AllowType == 0).map(c => c.Name),
                    DeniedIPs: res.filter(c => c.DataType == 2 && c.AllowType == 1).map(c => c.Name),
                    DomainKill: state.domainKill
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
                    devices: _devices.map(c => c.MachineName),
                    data: rules.list,
                    ids1: rules.ids1,
                    ids2: rules.ids2,
                }).then((errorDevices) => {
                    state.loading = false;
                    if (errorDevices && errorDevices.length > 0) {
                        ElMessage.error(`操作失败，失败设备:${errorDevices.join(',')}`);
                    } else {
                        ElMessage.success('操作成功！');
                        globalData.value.devices.filter(c => _devices.indexOf(c.MachineName) >= 0).forEach(device => {
                            device.Hijack.RuleIds1 = rules.ids1;
                            device.Hijack.RuleIds2 = rules.ids2;
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
        const parseDomainKill = () => {
            state.domainKill = pluginState.value.hijack.devices.filter(c => c.Hijack.DomainKill === true).length > 0;
        }
        const parseItems = (devices)=>{
            let ids1 = devices.reduce((arr, value) => {
                arr.push(...value.Hijack.RuleIds1);
                return arr;
            }, []);
            let ids2 = devices.reduce((arr, value) => {
                arr.push(...value.Hijack.RuleIds2);
                return arr;
            }, []);
            state.currentPrivate = state.privateRules.filter(c => ids1.indexOf(c.Name) >= 0);
            state.currentPublic = state.publicRules.filter(c => ids2.indexOf(c.Name) >= 0);
            state.items = devices;
        }
        const handleDevicesChange = (devices) => {
            parseDomainKill();
            parseItems(devices);
        }
        onMounted(() => {
            parseDomainKill();
            parseItems( pluginState.value.hijack.devices);
        });

        return {
            state, globalData, devices, privateRules, publicRules, handleSubmit, handleCancel, handleDevicesChange
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