<template>
    <div>
        <el-button-group>
            <el-button size="small">打洞</el-button>
            <el-button size="small">中继</el-button>
        </el-button-group>
        <!-- <el-dropdown size="small">
            <span class="el-dropdown-link">
                测试
                <el-icon class="el-icon--right">
                    <arrow-down />
                </el-icon>
            </span>
            <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item @click="handleTestTunnel(data.MachineName)">打洞</el-dropdown-item>
                    <el-dropdown-item @click="handleTestRelay(data.MachineName)">中继</el-dropdown-item>
                </el-dropdown-menu>
            </template>
</el-dropdown> -->
    </div>
</template>
<script>
import { updateRelayConnect } from '@/apis/relay';
import { updateTunnelConnect } from '@/apis/tunnel';
import { ElMessage } from 'element-plus';

export default {
    props: ['data'],
    setup(props) {

        const handleTestTunnel = (name) => {
            updateTunnelConnect(name).then(() => {
                ElMessage.success('已操作，请去检查日志')
            }).catch(() => { });
        }
        const handleTestRelay = (name) => {
            updateRelayConnect(name).then(() => {
                ElMessage.success('已操作，请去检查日志')
            }).catch(() => { });;
        }

        return {
            data: props.data, handleTestTunnel, handleTestRelay
        }
    }
}
</script>
<style lang="stylus" scoped>

</style>