<template>
    <el-col :span="3">
        <el-switch class="usb" size="small" @click="handleUSB" disabled :model-value="usb" inline-prompt active-text="U盘" inactive-text="U盘" />
    </el-col>
    <el-col :span="3">
        <el-switch class="usb" size="small" @click="handleSetting" disabled :model-value="setting" inline-prompt active-text="设置" inactive-text="设置" />
    </el-col>
    <el-col :span="3">
        <el-switch class="usb" size="small" @click="handleShutdown" disabled :model-value="shutdown" inline-prompt active-text="关机" inactive-text="关机" />
    </el-col>
</template>

<script>
import { injectPluginState } from '../../provide'
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed } from 'vue';
import { updateRegistryOptions } from '@/apis/system';
export default {
    sort: 4,
    props: ['data'],
    setup(props) {

        const data = props.data;
        const pluginState = injectPluginState();
        const usb = computed(() => {
            if (data.System.OptionValues && data.System.OptionKeys.USBSTOR) {
                return data.System.OptionValues[data.System.OptionKeys.USBSTOR.Index] == '1';
            }
            return false;
        });
        const setting = computed(() => {
            if (data.System.OptionValues && data.System.OptionKeys.NoControlPanel) {
                return data.System.OptionValues[data.System.OptionKeys.NoControlPanel.Index] == '1';
            }
            return false;
        });
        const shutdown = computed(() => {
            if (data.System.OptionValues && data.System.OptionKeys.NoClose) {
                return data.System.OptionValues[data.System.OptionKeys.NoClose.Index] == '1';
            }
            return false;
        });
        const handleChange = (key, desc, value) => {
            return new Promise((resolve, reject) => {
                ElMessageBox.confirm(desc, '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    updateRegistryOptions([props.data.MachineName], [key], value).then((res) => {
                        if (res) {
                            ElMessage.success('操作成功');
                            resolve();
                        } else {
                            ElMessage.error('操作失败');
                            reject();
                        }
                    }).catch(() => {
                        ElMessage.error('操作失败');
                        reject();
                    });

                }).catch(() => {
                    reject();
                });
            });
        }

        const handleUSB = () => {
            handleChange('USBSTOR', usb.value ? '确定启用USB吗？' : '确定禁用USB吗？', !usb.value).then(() => { }).catch(() => { });

        }
        const handleSetting = () => {
            handleChange('NoControlPanel', setting.value ? '确定启用设置吗？' : '确定禁用设置吗？', !setting.value).then(() => { }).catch(() => { });
        }
        const handleShutdown = () => {
            const newValue = !shutdown.value;
            handleChange('NoClose', shutdown.value ? '确定启用关机按钮吗？' : '确定禁用关机按钮吗？', newValue).then(() => {
                updateRegistryOptions([props.data.MachineName], [
                    'NoLogOff', 'DisableLockWorkstation', 'HideFastUserSwitching', 'DisableChangePassword'
                ], newValue);
            }).catch(() => { });
        }

        return {
            data, usb, setting, shutdown, handleUSB, handleSetting, handleShutdown
        }
    }
}
</script>

<style lang="stylus" scoped>
// var (--el-switch-off-color)
.el-switch.usb {
    // --el-switch-off-color: rgba(255, 255, 255, 0.1);
    --el-switch-on-color: rgba(255, 0, 0, 0.8) !important;
}
</style>