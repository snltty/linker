<template>
    <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap"
        :title="`设置[${state.machineName}]组网`" top="1vh" width="760">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="140">
                <el-form-item prop="gateway" style="margin-bottom:0">
                    赐予此设备IP，其它设备可通过此IP访问
                </el-form-item>
                <el-form-item label="此设备的虚拟网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" style="width:14rem" />
                    <span>/</span>
                    <el-input @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength"
                        style="width:4rem" />
                    <span style="width: 2rem;"></span>
                    <el-checkbox v-model="state.ruleForm.ShowDelay" label="显示延迟" size="large"
                        style="margin-right:1rem" />
                    <el-checkbox v-model="state.ruleForm.AutoConnect" label="自动连接" size="large"
                        style="margin-right:1rem" />
                    <el-checkbox v-model="state.ruleForm.Multicast" label="禁用UDP广播" size="large" />
                </el-form-item>
                <el-form-item prop="upgrade" style="margin-bottom:0">
                    <el-checkbox v-model="state.ruleForm.Upgrade" label="我很懂，我要使用高级功能(点对网和网对网)" size="large" />
                </el-form-item>
                <div class="upgrade-wrap" v-if="state.ruleForm.Upgrade">
                    <el-form-item prop="nat" style="margin-bottom:0">
                        <span class="yellow">此设备能使用NAT转发，只需局域网IP，剩下的交给NAT(linux、macos、win10+)</span>
                    </el-form-item>
                    <el-form-item label="此设备局域网IP" prop="LanIP" style="border-bottom: 1px solid #ddd;margin-bottom:0">
                        <template v-for="(item, index) in state.ruleForm.Lans" :key="index">
                            <div class="flex" style="margin-bottom:.6rem">
                                <div class="flex-1">
                                    <el-input v-model="item.IP" style="width:14rem" />
                                    <span>/</span>
                                    <el-input @change="handleMaskChange(index)" v-model="item.PrefixLength"
                                        style="width:4rem" />
                                </div>
                                <div class="pdl-10">
                                    <el-checkbox v-model="item.Disabled" label="禁用记录" size="large" />
                                </div>
                                <div class="pdl-10">
                                    <el-button type="danger" @click="handleDel(index)"><el-icon>
                                            <Delete />
                                        </el-icon></el-button>
                                    <el-button type="primary" @click="handleAdd(index)"><el-icon>
                                            <Plus />
                                        </el-icon></el-button>
                                </div>
                            </div>
                        </template>
                    </el-form-item>
                    <el-form-item prop="forward" style="margin-bottom:0">
                        <div>
                            <span class="yellow">此设备无法使用NAT转发，或只想使用端口转发</span>
                            <el-button size="small" @click="handleRefreshForward()">
                                <el-icon>
                                    <Refresh />
                                </el-icon>
                            </el-button>
                        </div>
                    </el-form-item>
                    <el-form-item label="端口转发" prop="forwards">
                        <template v-for="(item, index) in state.ruleForm.Forwards" :key="index">
                            <div class="flex w-100" style="margin-bottom:.2rem;padding-right:.6rem">
                                <div :title="item.Error" :class="{ red: !!item.Error }"><strong>{{ index + 1
                                        }}、</strong></div>
                                <div>
                                    <el-input @change="handleForwardChange(index)" v-model="item.ListenPort"
                                        style="width:6rem" />
                                    -> <el-input v-model="item.ConnectAddr" style="width:12rem" />
                                    : <el-input @change="handleForwardChange(index)" v-model="item.ConnectPort"
                                        style="width:6rem" />
                                    &nbsp;<el-input v-model="item.Remark" style="width:16rem" placeholder="备注" />
                                </div>
                                <div class="pdl-10">
                                    <el-button type="danger" @click="handleDelForward(index)">
                                        <el-icon>
                                            <Delete />
                                        </el-icon>
                                    </el-button>
                                    <el-button type="primary" @click="handleAddForward(index)">
                                        <el-icon>
                                            <Plus />
                                        </el-icon>
                                    </el-button>

                                </div>
                            </div>
                        </template>
                    </el-form-item>
                </div>
                <el-form-item label="" prop="Btns">
                    <div>
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import { updateTuntap, subscribeForwardTest } from '@/apis/tuntap';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch, onMounted, onUnmounted } from 'vue';
import { useTuntap } from './tuntap';
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { Delete, Plus, Warning, Refresh },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const tuntap = useTuntap();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            machineName: tuntap.value.current.device.MachineName,
            bufferSize: globalData.value.bufferSize,
            ruleForm: {
                IP: tuntap.value.current.IP,
                Lans: tuntap.value.current.Lans.slice(0),
                PrefixLength: tuntap.value.current.PrefixLength || 24,
                Gateway: tuntap.value.current.Gateway,
                ShowDelay: tuntap.value.current.ShowDelay,
                AutoConnect: tuntap.value.current.AutoConnect,
                Upgrade: tuntap.value.current.Upgrade,
                Multicast: tuntap.value.current.Multicast,

                Forwards: tuntap.value.current.Forwards.length == 0 ? [
                    { ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' }
                ] : tuntap.value.current.Forwards.slice(0)
            },
            rules: {},
            timer: 0
        });
        if (state.ruleForm.Lans.length == 0) {
            state.ruleForm.Lans.push({ IP: '0.0.0.0', PrefixLength: 24 });
        }
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handlePrefixLengthChange = () => {
            var value = +state.ruleForm.PrefixLength;
            if (value > 32 || value < 16 || isNaN(value)) {
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
        }

        const handleMaskChange = (index) => {
            var value = +state.ruleForm.Lans[index].PrefixLength;
            if (value > 32 || value < 16 || isNaN(value)) {
                value = 24;
            }
            state.ruleForm.Lans[index].PrefixLength = value;
        }
        const handleDel = (index) => {
            state.ruleForm.Lans.splice(index, 1);
            if (state.ruleForm.Lans.length == 0) {
                handleAdd(0);
            }
        }

        const handleAdd = (index) => {
            state.ruleForm.Lans.splice(index + 1, 0, { IP: '0.0.0.0', PrefixLength: 24 });
        }

        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tuntap.value.current));
            json.IP = state.ruleForm.IP.replace(/\s/g, '') || '0.0.0.0';
            json.Lans = state.ruleForm.Lans.map(c => { c.PrefixLength = +c.PrefixLength; return c; });
            json.PrefixLength = +state.ruleForm.PrefixLength;
            json.Gateway = state.ruleForm.Gateway;
            json.ShowDelay = state.ruleForm.ShowDelay;
            json.AutoConnect = state.ruleForm.AutoConnect;
            json.Upgrade = state.ruleForm.Upgrade;
            json.Multicast = state.ruleForm.Multicast;
            json.Forwards = state.ruleForm.Forwards;
            json.Forwards.forEach(c => {
                c.ListenPort = +c.ListenPort;
                c.ConnectPort = +c.ConnectPort;
            });
            updateTuntap(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }

        const handleDelForward = (index) => {
            state.ruleForm.Forwards.splice(index, 1);
            if (state.ruleForm.Forwards.length == 0) {
                handleAddForward(0);
            }
        }
        const handleAddForward = (index) => {
            state.ruleForm.Forwards.splice(index + 1, 0, { ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' });
        }
        const handleForwardChange = () => {

        }
        const _subscribeForwardTest = () => {
            clearTimeout(state.timer);
            subscribeForwardTest(tuntap.value.current.MachineId).then(() => {
                state.timer = setTimeout(_subscribeForwardTest, 3000);
            }).catch(() => { });
        }
        const handleRefreshForward = () => {
            ElMessage.success('已刷新');

            const list = tuntap.value.list[tuntap.value.current.MachineId];
            state.ruleForm.Forwards = list.length == 0 ? [
                { ListenAddr: '0.0.0.0', ListenPort: 0, ConnectAddr: '0.0.0.0', ConnectPort: 0, Remark: '' }
            ] : list.Forwards.slice(0);
        }

        onMounted(() => {
            _subscribeForwardTest();
        });
        onUnmounted(() => {
            clearTimeout(state.timer);
        });

        return {
            state, ruleFormRef, handlePrefixLengthChange, handleMaskChange, handleDel, handleAdd, handleSave,
            handleForwardChange, handleDelForward, handleAddForward, handleRefreshForward
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}

.upgrade-wrap{
    border:1px solid #ddd;
    margin-bottom:2rem
    padding:0 0 1rem 0;
}
</style>