<template>
    <div class="servers-wrap">
        <el-tabs type="border-card" style="width:100%" v-model="state.tab">
            <el-tab-pane label="信标服务器" name="login">
                <SignInServers v-if="state.connected"></SignInServers>
            </el-tab-pane>
            <el-tab-pane label="公网端口服务器" name="ip">
                <TunnelServers v-if="state.connected"></TunnelServers>
            </el-tab-pane>
            <el-tab-pane label="打洞协议" name="hole">
                <Transports v-if="state.connected"></Transports>
            </el-tab-pane>
            <el-tab-pane label="中继服务器" name="relay">
                <RelayServers v-if="state.connected"></RelayServers>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import SignInServers from './SignInServers.vue'
import RelayServers from './RelayServers.vue'
import TunnelServers from './TunnelServers.vue'
import Transports from './Transports.vue'
import { injectGlobalData } from '@/provide';
export default {
    components:{SignInServers,RelayServers,TunnelServers,Transports},
    setup(props) {
        
        const globalData = injectGlobalData();
        const state = reactive({
            tab:'login',
            connected:computed(()=>globalData.value.connected && globalData.value.configed)
        });

        return {
            state
            
        }
    }
}
</script>
<style lang="stylus" scoped>
.servers-wrap{
    padding:1rem
    font-size:1.3rem;
    color:#555;
    a{color:#333;}
}

</style>