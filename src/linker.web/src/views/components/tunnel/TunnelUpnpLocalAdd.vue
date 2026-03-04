<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="UPNP" width="360" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="设备类型" prop="DeviceType">
                    <el-select v-model="state.ruleForm.DeviceType" placeholder="请选择">
                        <el-option v-for="(item,key) in deviceTypes" :key="Number(key)" :label="item" :value="Number(key)"></el-option>
                    </el-select>
                </el-form-item>
                <el-form-item label="协议" prop="ProtocolType">
                    <el-select v-model="state.ruleForm.ProtocolType" placeholder="请选择">
                        <el-option v-for="(item,key) in protocolTypes" :key="Number(key)" :label="item" :value="Number(key)"></el-option>
                    </el-select>
                </el-form-item>
                <el-form-item label="外网端口" prop="PublicPort">
                    <el-input-number v-model="state.ruleForm.PublicPort" />
                </el-form-item>
                <el-form-item label="外网端口" prop="PrivatePort">
                    <el-input-number v-model="state.ruleForm.PrivatePort" />
                </el-form-item>
                <el-form-item label="存活时间" prop="LeaseDuration">
                    <el-input-number v-model="state.ruleForm.LeaseDuration" />
                </el-form-item>
                <el-form-item label="描述" prop="Description">
                    <el-input v-model="state.ruleForm.Description" :maxlength="32" show-word-limit clearable />
                </el-form-item>
                <el-form-item label="启用" prop="Enabled">
                    <el-switch v-model="state.ruleForm.Enabled" />
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
import { addUpnpMappingInfo, delUpnpMappingInfo } from '@/apis/tunnel';
import { ElMessage } from 'element-plus';
import { inject, reactive, ref, watch } from 'vue';

export default {
    props: ['modelValue','deviceTypes','protocolTypes','machineId'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

        const ruleFormRef = ref(null);
        const addState = inject('addState');  
        const deletePort = addState.value.PublicPort || 0;
        const deleteProtocolType = addState.value.ProtocolType || 0;
        
        const state = reactive({
            show: true,
            ruleForm: {
                PublicPort: addState.value.PublicPort || 0,
                PrivatePort: addState.value.PrivatePort || 0,
                ProtocolType: addState.value.ProtocolType || 6,
                Enabled: addState.value.Enabled,
                Description: addState.value.Description || '',
                LeaseDuration: addState.value.LeaseDuration || 7200,
                DeviceType: addState.value.DeviceType || 255,
                Deletable: true,
            },
            rules: {
                PublicPort: [
                    { required: true, message: '请输入外网端口', trigger: 'blur' },
                    { type: 'number',min: 1, max: 65535, message: '请输入数字1-65535', trigger: ['blur', 'change'] },
                ],
                PrivatePort: [
                    { required: true, message: '请输入内网端口', trigger: 'blur' },
                    { type: 'number',min: 1, max: 65535, message: '请输入数字1-65535', trigger: ['blur', 'change'] }
                ],
                LeaseDuration: [
                    { required: true, message: '请输入存活时间', trigger: 'blur' },
                    { type: 'number',min: 0, max: 2147483647, message: '请输入数字0-2147483647', trigger: ['blur', 'change'] }
                ]
            },
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
            ruleFormRef.value.validate((valid) => {
                if(!valid) return;
                    
                const json = JSON.parse(JSON.stringify(state.ruleForm));
                json.PublicPort = +state.ruleForm.PublicPort;
                json.PrivatePort = +state.ruleForm.PrivatePort;
                json.ProtocolType = +state.ruleForm.ProtocolType;
                json.LeaseDuration = +state.ruleForm.LeaseDuration;
                json.DeviceType = +state.ruleForm.DeviceType;

                delUpnpMappingInfo(props.machineId,deletePort,deleteProtocolType).then(() => {
                    addUpnpMappingInfo(props.machineId,json).then(() => {
                        state.show = false;
                        ElMessage.success('已操作！');
                        emit('change')
                    }).catch((err) => {
                        console.log(err);
                        ElMessage.error('操作失败！');
                    });
                }).catch((err) => {
                    console.log(err);
                    ElMessage.error('操作失败！');
                });
            });
        }
        return {
           state, ruleFormRef, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>