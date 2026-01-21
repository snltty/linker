<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" :title="`设置[${state.machineName}]网关`" width="560" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="" prop="alert">
                    <div>网关层级为你的设备与外网的距离，你可以手动调整数值</div>
                </el-form-item>
                <el-form-item label="">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-form-item label="网关层级" prop="RouteLevel">
                                <el-input v-trim readonly v-model="state.ruleForm.RouteLevel" style="width:15rem" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="加上" prop="RouteLevelPlus">
                                <el-input-number v-model="state.ruleForm.RouteLevelPlus" />
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-form-item label="外网端口" prop="PortMapWan">
                                <el-input-number v-model="state.ruleForm.PortMapWan" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="内网端口" prop="PortMapLan">
                                <el-input-number v-model="state.ruleForm.PortMapLan" />
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-form-item label="入口IP" prop="InIp">
                                <el-input v-model="state.ruleForm.InIp" style="width:15rem"/>
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <span>入口ip与出口ip不一致时填写</span>
                        </el-col>
                    </el-row>
                </el-form-item>
                
                <el-form-item label="" prop="alert" v-if="state.net.HostName">
                    <div>
                        <h3>{{ state.net.HostName }}</h3>
                        <ul>
                            <template v-for="(item,index) in state.net.Lans.filter(c=>c.Ips.length > 0)">
                                <li>
                                    <div>【{{ item.Mac||'00-00-00-00-00-00' }}】{{ item.Desc }}</div>
                                    <div class="pdl-20">{{ item.Ips.join('、') }}</div>
                                </li>
                            </template>
                        </ul>
                        <h3>跳跃点</h3>
                        <div class="pdl-20">{{ state.net.Routes.join('、') }}</div>
                    </div>
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
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

export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

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
                InIp: tunnel.value.current.InIp
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
            setTunnelRouteLevel(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch((err) => {
                console.log(err);
                ElMessage.error('操作失败！');
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