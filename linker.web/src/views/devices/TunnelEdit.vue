<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="设置隧道" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="" prop="alert">
                    <div>
                        <p>网关层级自动计算，可能并不符合实际情况</p>
                        <p>你可以调整层级加减，得到最终层级数以符合你的实际环境</p>
                        <p>这有助于提高TCP打洞成功率</p>
                    </div>
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
            ruleForm: {
                RouteLevel: tunnel.value.current.RouteLevel,
                RouteLevelPlus: tunnel.value.current.RouteLevelPlus,
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