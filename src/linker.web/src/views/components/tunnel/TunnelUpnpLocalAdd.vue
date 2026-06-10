<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" title="UPNP" width="360" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('network.upnp.type')" prop="DeviceType">
                    <el-select v-model="state.ruleForm.DeviceType">
                        <el-option v-for="(item,key) in deviceTypes" :key="Number(key)" :label="item" :value="Number(key)"></el-option>
                    </el-select>
                </el-form-item>
                <el-form-item :label="$t('network.upnp.proto')" prop="ProtocolType">
                    <el-select v-model="state.ruleForm.ProtocolType">
                        <el-option v-for="(item,key) in protocolTypes" :key="Number(key)" :label="item" :value="Number(key)"></el-option>
                    </el-select>
                </el-form-item>
                <el-form-item :label="$t('network.upnp.pport')" prop="PublicPort">
                    <el-input-number v-model="state.ruleForm.PublicPort" />
                </el-form-item>
                <el-form-item :label="$t('network.upnp.lport')" prop="PrivatePort">
                    <el-input-number v-model="state.ruleForm.PrivatePort" />
                </el-form-item>
                <el-form-item :label="$t('network.upnp.alive')" prop="LeaseDuration">
                    <el-input-number v-model="state.ruleForm.LeaseDuration" />
                </el-form-item>
                <el-form-item :label="$t('network.upnp.desc')" prop="Description">
                    <el-input v-model="state.ruleForm.Description" :maxlength="32" show-word-limit clearable />
                </el-form-item>
                <el-form-item :label="$t('network.upnp.enabled')" prop="Enabled">
                    <el-switch v-model="state.ruleForm.Enabled" />
                </el-form-item>
                
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
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
import { useI18n } from 'vue-i18n';

export default {
    props: ['modelValue','deviceTypes','protocolTypes','machineId'],
    emits: ['change','update:modelValue'],
    setup(props, { emit }) {

        const { t } = useI18n();
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
                    { required: true, message: t('common.required'), trigger: 'blur' },
                    { type: 'number',min: 1, max: 65535, message: '1-65535', trigger: ['blur', 'change'] },
                ],
                PrivatePort: [
                    { required: true, message: t('common.required'), trigger: 'blur' },
                    { type: 'number',min: 1, max: 65535, message: '1-65535', trigger: ['blur', 'change'] }
                ],
                LeaseDuration: [
                    { required: true, message: t('common.required'), trigger: 'blur' },
                    { type: 'number',min: 0, max: 2147483647, message: '0-2147483647', trigger: ['blur', 'change'] }
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
                        ElMessage.success(t('common.opered'));
                        emit('change')
                    }).catch((err) => {
                        console.log(err);
                        ElMessage.error(t('common.operFail'));
                    });
                }).catch((err) => {
                    console.log(err);
                    ElMessage.error(t('common.operFail'));
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