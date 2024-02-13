<template>
    <el-col :span="3">
        <el-switch class="volume" size="small" @click="handleVolumeMute" :model-value="data.Volume.Mute" inline-prompt active-text="静音" inactive-text="静音" />
    </el-col>
</template>

<script>
import { setVolumeMute } from '@/apis/volume';
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide';
export default {
    sort: 0,
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleVolumeMute = () => {
            let desc = props.data.Volume.Mute ? '确定取消静音吗？' : '确定静音吗？';
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                setVolumeMute([props.data.MachineName], !props.data.Volume.Mute).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败');
                    }
                }).catch((e) => {
                    ElMessage.error('操作失败');
                });

            }).catch(() => { });
        }


        return {
            data: props.data, handleVolumeMute
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-switch.volume {
    // --el-switch-off-color: rgba(255, 255, 255, 0.1);
    --el-switch-on-color: rgba(33, 153, 33, 0.8);
}
</style>