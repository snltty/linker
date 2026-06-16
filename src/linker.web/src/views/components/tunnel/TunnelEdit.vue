<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" :title="`设置[${state.machineName}]网关`" width="560" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="" prop="">
                    <div class="w-100">
                        <el-descriptions :column="2" size="small" border >
                            <el-descriptions-item :label="$t('network.level')">
                                    <el-input v-trim readonly v-model="state.ruleForm.RouteLevel" class="w-15" />
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.plus')">
                                <el-input-number v-model="state.ruleForm.RouteLevelPlus" />
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.upnp.pport')">
                                <el-input-number v-model="state.ruleForm.PortMapWan" />
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.upnp.lport')">
                                <el-input-number v-model="state.ruleForm.PortMapLan" />
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.inip')" span="2">
                                <el-input v-model="state.ruleForm.InIp" class="w-15"/>
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.mesh.bandwidth')">
                                <el-input v-model="state.ruleForm.Mesh.Bandwidth" class="w-10"/>Mbps
                            </el-descriptions-item>
                            <el-descriptions-item :label="$t('network.mesh.enabled')">
                                <el-checkbox v-model="state.ruleForm.Mesh.Enabled">{{ $t('network.mesh.enabled') }}</el-checkbox>
                            </el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-form-item>
                <el-form-item label="" prop="alert" v-if="state.net.HostName">
                    <div class="w-100">
                        <el-descriptions :column="2" size="small" border :title="state.net.HostName" >
                            <template v-for="(item,index) in state.net.Lans.filter(c=>c.Ips.length > 0)">
                                <el-descriptions-item :label="$t('network.name')">{{ item.Desc }}</el-descriptions-item>
                                <el-descriptions-item :label="$t('network.mac')">{{ item.Mac||'00-00-00-00-00-00' }}</el-descriptions-item>
                                <el-descriptions-item :label="$t('network.ip')" :span="2">{{ item.Ips.join('、') }}</el-descriptions-item>
                            </template>
                            <el-descriptions-item :label="$t('network.jump')">{{ state.net.Routes.join('、') }}</el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{ $t('common.cancel') }}</el-button>
                        <el-button type="primary" @click="handleSave">{{ $t('common.confirm') }}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import {getTunnelNetwork, setTunnelRouteLevel } from '@/apis/tunnel';
import { ElMessage } from 'element-plus';
import { onMounted, reactive, ref, watch } from 'vue';
import { useTunnel } from './tunnel';
import { useI18n } from 'vue-i18n';

export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

        const {t} = useI18n();
        const tunnel = useTunnel();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            machineName: tunnel.value.current.device.MachineName,
            ruleForm: {
                RouteLevel: tunnel.value.current.RouteLevel,
                RouteLevelPlus: tunnel.value.current.RouteLevelPlus,
                PortMapWan: tunnel.value.current.PortMapWan,
                PortMapLan: tunnel.value.current.PortMapLan,
                InIp: tunnel.value.current.InIp,
                Mesh:{
                    Enabled:tunnel.value.current.Mesh.Enabled,
                    Bandwidth:tunnel.value.current.Mesh.Bandwidth,
                }
            },
            rules: {},
            net:{}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tunnel.value.current,(key,value)=> key =='device'?'':value));
            json.RouteLevel = +state.ruleForm.RouteLevel;
            json.RouteLevelPlus = +state.ruleForm.RouteLevelPlus;
            json.PortMapWan = +state.ruleForm.PortMapWan;
            json.PortMapLan = +state.ruleForm.PortMapLan;
            json.InIp = state.ruleForm.InIp;
            json.Mesh.Bandwidth = +state.ruleForm.Mesh.Bandwidth;
            json.Mesh.Enabled = state.ruleForm.Mesh.Enabled;
            setTunnelRouteLevel(json).then(() => {
                state.show = false;
                ElMessage.success(t('common.opered'));
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }


        onMounted(()=>{
            getTunnelNetwork(tunnel.value.current.MachineId).then((res)=>{
                state.net = res;
            }).catch(()=>{})
        })

        return {
           state, ruleFormRef,  handleSave,tunnel
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
ul{
    li{
        padding-left:2rem
    }
}
</style>