<template>
    <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap"
        :title="`设置[${state.machineName}]组网`" top="1vh" width="760">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="8rem">
                <el-form-item label="网卡名" prop="Name">
                    <el-input v-model="state.ruleForm.Name" style="width:14rem" /> <span>留空则使用【本组网络】的设置</span>
                </el-form-item>
                <el-form-item label="网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" style="width:14rem" />
                    <span>/</span>
                    <el-input @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
                    <span style="width: 2rem;"></span>
                    <el-checkbox v-model="state.ruleForm.ShowDelay" label="显示延迟" size="large" style="margin-right:1rem" />
                    <el-checkbox v-model="state.ruleForm.AutoConnect" label="自动连接" size="large" style="margin-right:1rem" />
                    <el-checkbox v-model="state.ruleForm.Multicast" label="禁用广播" size="large" />
                    <el-checkbox v-model="state.ruleForm.Nat" label="禁用NAT" size="large" />
                </el-form-item>
                <el-form-item prop="upgrade" class="mgb-0">
                    <el-checkbox v-model="state.ruleForm.Upgrade" label="我很懂，我要使用高级功能(点对网和网对网)" size="large" />
                </el-form-item>
                <div class="upgrade-wrap" v-if="state.ruleForm.Upgrade">
                    <el-form-item label="局域网IP" prop="LanIP" class="m-b-0" style="border-bottom: 1px solid #ddd;">
                        <TuntapLan ref="lanDom"></TuntapLan>
                    </el-form-item>
                    <el-form-item label="端口转发" prop="forwards">
                        <TuntapForward ref="forwardDom"></TuntapForward>
                    </el-form-item>
                </div>
                <el-form-item label="" prop="Btns" label-width="0">
                    <div class="w-100 t-c">
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import { updateTuntap } from '@/apis/tuntap';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch} from 'vue';
import { useTuntap } from './tuntap';
import TuntapForward from './TuntapForward.vue'
import TuntapLan from './TuntapLan.vue'
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change', 'update:modelValue'],
    components: { Delete, Plus, Warning, Refresh,TuntapForward ,TuntapLan},
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
                PrefixLength: tuntap.value.current.PrefixLength || 24,
                Gateway: tuntap.value.current.Gateway,
                ShowDelay: tuntap.value.current.ShowDelay,
                AutoConnect: tuntap.value.current.AutoConnect,
                Upgrade: tuntap.value.current.Upgrade,
                Multicast: tuntap.value.current.Multicast,
                Nat: tuntap.value.current.Nat,
                Forwards: tuntap.value.current.Forwards,
                Name: tuntap.value.current.Name,
            },
            rules: {
                Name: {
                    type: 'string',
                    pattern: /^$|^[A-Za-z][A-Za-z0-9]{0,31}$/,
                    message:'请输入正确的网卡名',
                    transform(value) {
                        return value.trim();
                    },
                }
            }
        });
        
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


        const lanDom = ref(null);
        const forwardDom = ref(null);
        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tuntap.value.current));
            json.IP = state.ruleForm.IP.replace(/\s/g, '') || '0.0.0.0';
            json.Lans = lanDom.value ?  lanDom.value.getData() : tuntap.value.current.Lans;
            json.PrefixLength = +state.ruleForm.PrefixLength;
            json.Gateway = state.ruleForm.Gateway;
            json.ShowDelay = state.ruleForm.ShowDelay;
            json.AutoConnect = state.ruleForm.AutoConnect;
            json.Upgrade = state.ruleForm.Upgrade;
            json.Multicast = state.ruleForm.Multicast;
            json.Nat = state.ruleForm.Nat;
            json.Forwards = forwardDom.value ?  forwardDom.value.getData() : tuntap.value.current.Forwards;
            json.Name = state.ruleForm.Name;
            updateTuntap(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
            });
        }

        return {
            state, ruleFormRef, handlePrefixLengthChange, handleSave,
            lanDom,forwardDom
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