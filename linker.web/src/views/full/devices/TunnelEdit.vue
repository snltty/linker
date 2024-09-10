<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" :title="`设置[${state.machineName}]网关`" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="" prop="alert">
                    <div>网关层级为你的设备与外网的距离，你可以手动调整数值</div>
                </el-form-item>
                <el-form-item label="">
                    <el-row>
                        <el-col :span="12">
                            <el-form-item label="网关层级" prop="RouteLevel">
                                <el-input readonly v-model="state.ruleForm.RouteLevel" />
                            </el-form-item>
                        </el-col>
                        <el-col :span="12">
                            <el-form-item label="调整层级" prop="RouteLevelPlus">
                                <el-input-number v-model="state.ruleForm.RouteLevelPlus" />
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" prop="alert">
                    <div>光猫映射了端口后，可以直接连接，把TcpPortMap调到第一</div>
                </el-form-item>
                <el-form-item label="">
                    <el-row>
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
import {setTunnelRouteLevel } from '@/apis/tunnel';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
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
            },
            rules: {}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tunnel.value.current));
            json.RouteLevel = +state.ruleForm.RouteLevel;
            json.RouteLevelPlus = +state.ruleForm.RouteLevelPlus;
            json.PortMapWan = +state.ruleForm.PortMapWan;
            json.PortMapLan = +state.ruleForm.PortMapLan;
            setTunnelRouteLevel(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }

        return {
           state, ruleFormRef,  handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
</style>