<template>
<div>
    <p><el-button size="small" @click="handleTestTunnel(data.MachineName)">测试打洞</el-button></p>
    <p><el-button size="small" @click="handleTestRelay(data.MachineName)">测试中继</el-button></p>
</div>
</template>
<script>
import { updateRelayConnect } from '@/apis/relay';
import { updateTunnelConnect } from '@/apis/tunnel';
import { ElMessage } from 'element-plus';

export default {
    props:['data'],
    setup(props) {

        const handleTestTunnel = (name)=>{
            updateTunnelConnect(name).then(()=>{
                ElMessage.success('已操作，请去检查日志')
            }).catch(()=>{});
        }
        const handleTestRelay = (name)=>{
            updateRelayConnect(name).then(()=>{
                ElMessage.success('已操作，请去检查日志')
            }).catch(()=>{});;
        }

        return {
            data:props.data,handleTestTunnel,handleTestRelay
        }
    }
}
</script>
<style lang="stylus" scoped>
    
</style>