<template>
    <div>
        <el-card shadow="never">
            <template #header>
                <div class="card-header">
                    <span>同步密钥</span>
                </div>
            </template>
            <div>
                同步，信标服务器，中继服务器，服务器代理穿透，的密钥到所有客户端
            </div>
            <template #footer>
                <div class="t-r">
                    <el-button type="success" @click="handleSyncSecretKey">确定同步</el-button>
                </div>
            </template>
        </el-card>
        <el-card shadow="never" style="margin-top:2rem">
            <template #header>
                <div class="card-header">
                    <span>同步服务器配置</span>
                </div>
            </template>
            <div>
                同步，信标服务器，端口服务器，中继服务器，列表到所有客户端 
            </div>
            <template #footer>
                <div class="t-r">
                    <el-button type="success" @click="handleSyncServer">确定同步</el-button>
                </div>
            </template>
        </el-card>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive } from 'vue'
import { setSecretKeyAsync, setServerAsync } from '@/apis/config';
export default {
    label:'同步配置',
    name:'async',
    order:7,
    setup(props) {
        const globalData = injectGlobalData();
        const state = reactive({});

        const handleSyncSecretKey = ()=>{

            const json = {
                SignSecretKey:globalData.value.config.Client.ServerInfo.SecretKey,
                RelaySecretKey:globalData.value.config.Client.Relay.Servers[0].SecretKey,
                SForwardSecretKey:globalData.value.config.Client.SForward.SecretKey
            }
            setSecretKeyAsync(json).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });;
        }
        const handleSyncServer = ()=>{
            const json = {
                SignServers:globalData.value.config.Client.Servers,
                RelayServers:globalData.value.config.Client.Relay.Servers,
                TunnelServers:globalData.value.config.Client.Tunnel.Servers
            }
            setServerAsync(json).then(()=>{
                ElMessage.success('已操作');
            }).catch(()=>{
                ElMessage.error('操作失败');
            });;
        }

        return {state,handleSyncSecretKey,handleSyncServer}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>