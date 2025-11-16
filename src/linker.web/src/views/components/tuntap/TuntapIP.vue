<template>
    <div class="wrap">
        <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="8rem">
            <el-form-item label="网卡ID" prop="Guid" v-if="state.showGuid">
                <el-input v-trim v-model="state.ruleForm.Guid" style="width:14rem" disabled />
                <el-button  @click="handleNewId" class="mgl-1">新ID</el-button>
            </el-form-item>
            <el-form-item label="网卡名" prop="Name">
                <el-input v-trim v-model="state.ruleForm.Name" style="width:14rem" />
                <span class="mgl-1">留空则使用【本组网络】的设置</span>
            </el-form-item>
            <el-form-item label="网卡IP" prop="IP" class="mgb-0">
                <el-input v-trim v-model="state.ruleForm.IP" style="width:14rem" />
                    <span>/</span>
                    <el-input v-trim @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
            </el-form-item>
            <el-form-item label="" class="mgb-0">
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.ShowDelay" label="显示延迟" size="large" />
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.AutoConnect" label="自动连接" size="large" />
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.Multicast" label="禁用广播" size="large" />
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.DisableNat" label="禁用NAT" size="large" />
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.InterfaceOrder" label="网卡顺序" size="large" />
                    <el-checkbox class="mgr-1" v-model="state.ruleForm.SrcProxy" label="TCP转代理" size="large" />
            </el-form-item>
        </el-form>
    </div>
</template>
<script>
import { onMounted, reactive, ref} from 'vue';
import { useTuntap } from './tuntap';
import TuntapForward from './TuntapForward.vue'
import TuntapLan from './TuntapLan.vue'
import { Delete, Plus, Warning, Refresh } from '@element-plus/icons-vue'
import { getid, setid } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
export default {
    emits: ['change'],
    components: { Delete, Plus, Warning, Refresh,TuntapForward ,TuntapLan},
    setup(props, { emit }) {
        

        const tuntap = useTuntap();

        const ruleFormRef = ref(null);
        const state = reactive({
            showGuid:tuntap.value.current.systems.indexOf('windows') >= 0,
            ruleForm: {
                IP: tuntap.value.current.IP,
                PrefixLength: tuntap.value.current.PrefixLength || 24,
                Gateway: tuntap.value.current.Gateway,
                ShowDelay: tuntap.value.current.ShowDelay,
                AutoConnect: tuntap.value.current.AutoConnect,
                Upgrade: tuntap.value.current.Upgrade,
                Multicast: tuntap.value.current.Multicast,
                DisableNat: tuntap.value.current.DisableNat,
                TcpMerge: tuntap.value.current.TcpMerge,
                InterfaceOrder: tuntap.value.current.InterfaceOrder,
                FakeAck: tuntap.value.current.FakeAck,
                SrcProxy: tuntap.value.current.SrcProxy,
                Forwards: tuntap.value.current.Forwards,
                Name: tuntap.value.current.Name,
                Guid: '',
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
        const handlePrefixLengthChange = () => {
            var value = +state.ruleForm.PrefixLength;
            if (value > 32 || value < 16 || isNaN(value)) {
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
        }
        const handleNewId = () => {
            state.ruleForm.Guid =  crypto.randomUUID();
            setid({key:tuntap.value.current.device.MachineId,value:state.ruleForm.Guid}).then(res=>{ 
                ElMessage.success('已操作')
            }).catch(()=>{
                ElMessage.error('操作失败！');
            });
        }

        const getData = ()=>{
            console.log(tuntap.value.current);
            const json = JSON.parse(JSON.stringify(tuntap.value.current,(key,value)=> key =='device'?'':value ));
            json.IP = state.ruleForm.IP.replace(/\s/g, '') || '0.0.0.0';
            json.PrefixLength = +state.ruleForm.PrefixLength;
            json.Gateway = state.ruleForm.Gateway;
            json.ShowDelay = state.ruleForm.ShowDelay;
            json.AutoConnect = state.ruleForm.AutoConnect;
            json.Upgrade = state.ruleForm.Upgrade;
            json.Multicast = state.ruleForm.Multicast;
            json.DisableNat = state.ruleForm.DisableNat;
            json.TcpMerge = state.ruleForm.TcpMerge;
            json.InterfaceOrder = state.ruleForm.InterfaceOrder;
            json.FakeAck = state.ruleForm.FakeAck;
            json.SrcProxy = state.ruleForm.SrcProxy;
            json.Name = state.ruleForm.Name;

            return json;
        }

        onMounted(()=>{
            getid(tuntap.value.current.device.MachineId).then(res=>{
                state.ruleForm.Guid = res;
            });
        })

        return {
            state, ruleFormRef, handlePrefixLengthChange,handleNewId,getData
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.wrap{min-height:40rem;}
</style>