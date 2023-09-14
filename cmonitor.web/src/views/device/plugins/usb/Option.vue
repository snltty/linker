<template>
    <el-col :span="4">
        <el-switch size="small" @click="handleUSB" :model-value="data.Usb.Value" inline-prompt active-color="#ff0000" active-text="U盘" inactive-text="U盘" />
    </el-col>
</template>

<script>
import { usbUpdate } from '@/apis/usb';
import { injectPluginState } from '../../provide'
import { ElMessage, ElMessageBox } from 'element-plus';
export default {
    sort: 4,
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleUSB = () => {
            let desc = props.data.Usb.Value ? '确定启用USB吗？' : '确定禁用USB吗？';
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                usbUpdate([props.data.MachineName], !props.data.Usb.Value).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败');
                    }
                }).catch(() => {
                    ElMessage.error('操作失败');
                });

            }).catch(() => { });
        }

        return {
            data: props.data, handleUSB
        }
    }
}
</script>

<style lang="stylus" scoped></style>