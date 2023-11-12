<template>
    <el-col :span="3">
        <el-switch class="llock" size="small" @click="handleLock" :model-value="data.LLock.LockScreen" inline-prompt active-text="锁屏" inactive-text="锁屏" />
    </el-col>
</template>

<script>
import { llockScreen } from '@/apis/llock';
import { injectPluginState } from '../../provide'
import { ElMessage, ElMessageBox } from 'element-plus';
export default {
    sort: 2,
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleLock = () => {
            let desc = props.data.LLock.LockScreen ? '确定解除锁屏吗？' : '确定开启锁屏吗？';
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                llockScreen([props.data.MachineName], !props.data.LLock.LockScreen).then((res) => {
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
            data: props.data, handleLock
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-switch.llock {
    // --el-switch-off-color: rgba(255, 255, 255, 0.1);
    --el-switch-on-color: rgba(33, 153, 33, 0.8);
}
</style>