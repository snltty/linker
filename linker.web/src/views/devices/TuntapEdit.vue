<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="设置虚拟网卡IP" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="80">
                <el-form-item label="网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" />
                </el-form-item>
                <el-form-item label="局域网IP" prop="LanIP">
                    <template v-for="(item, index) in state.ruleForm.LanIPs" :key="index">
                        <div class="flex" style="margin-bottom:.6rem">
                            <div class="flex-1">
                                <el-input v-model="state.ruleForm.LanIPs[index]" />
                            </div>
                            <div class="pdl-10">
                                <el-button type="danger" @click="handleDel(index)"><el-icon><Delete /></el-icon></el-button>
                                <el-button type="primary" @click="handleAdd(index)"><el-icon><Plus /></el-icon></el-button>
                            </div>
                        </div>
                    </template>
                </el-form-item>
                <el-form-item label="" prop="alert">
                    <div>网卡IP和局域网IP都是 /24 掩码</div>
                </el-form-item>
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
import {updateTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { inject, reactive, ref, watch } from 'vue';

export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {
        const tuntap = inject('tuntap');
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                IP: tuntap.value.current.IP,
                LanIPs: tuntap.value.current.LanIPs.slice(0)
            },
            rules: {}
        });
        if (state.ruleForm.LanIPs.length == 0) {
            state.ruleForm.LanIPs.push('');
        }
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleDel = (index) => {
            if (state.ruleForm.LanIPs.length == 1) return;
            state.ruleForm.LanIPs.splice(index, 1);
        }
        const handleAdd = (index) => {
            state.ruleForm.LanIPs.splice(index + 1, 0, '');
        }
        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tuntap.value.current));
            json.IP = state.ruleForm.IP || '0.0.0.0';
            json.LanIPs = state.ruleForm.LanIPs.filter(c => c);
            updateTuntap(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }

        return {
           state, ruleFormRef,  handleDel, handleAdd, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.green{color:green;}
.el-switch.is-disabled{opacity :1;}
</style>