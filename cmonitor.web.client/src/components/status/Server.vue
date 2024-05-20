<template>
    <div class="status-server-wrap" :class="{connected:state.connected}">
        <a href="javascript:;" @click="handleConfig">服务器 {{state.server}}</a>
        <span class="num">{{state.serverLength}}</span>
    </div>
    <el-dialog v-model="state.show" title="登入设置" width="700">
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="6rem">
                <el-form-item label=""  label-width="0">
                    <el-col :span="12">
                        <el-form-item label="机器名" prop="name">
                            <el-input v-model="state.form.name" maxlength="12" show-word-limit />
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="分组名" prop="groupid">
                            <el-input v-model="state.form.groupid" maxlength="32" show-word-limit />
                        </el-form-item>
                    </el-col>
                </el-form-item>
                <el-form-item label-width="0">
                    <el-tabs type="border-card" style="width:100%" v-model="state.tab">
                        <el-tab-pane label="登入服务器" name="login">
                            <Servers :data="state.servers"></Servers>
                        </el-tab-pane>
                        <el-tab-pane label="中继服务器" name="relay">
                            <RelayServers :data="state.relayServers"></RelayServers>
                        </el-tab-pane>
                        <el-tab-pane label="公网端口服务器" name="hole">
                            <TunnelServers :data="state.holeServers"></TunnelServers>
                        </el-tab-pane>
                    </el-tabs>
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
        <div class="dialog-footer t-c">
            <el-button @click="state.show = false" :loading="state.loading">取消</el-button>
            <el-button type="primary" @click="handleSave" :loading="state.loading">确定保存</el-button>
        </div>
        </template>
    </el-dialog>

    
</template>
<script>
import { updateConfigSet, updateConfigSetServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue';
import Servers from './Servers.vue'
import RelayServers from './RelayServers.vue'
import TunnelServers from './TunnelServers.vue'
export default {
    components:{Servers,RelayServers,TunnelServers},
    setup(props) {
        
        const globalData = injectGlobalData();

        const state = reactive({
            show:false,
            loading:false,
            connected:computed(()=>globalData.value.signin.Connected),
            connecting:computed(()=>globalData.value.signin.Connecting),
            server:computed(()=>globalData.value.config.Client.Server),
            serverLength:computed(()=>(globalData.value.config.Client.Servers||[]).length),
            form:{
                name:globalData.value.config.Client.Name,
                groupid:globalData.value.config.Client.GroupId,
            },
            rules:{},

            tab:'login',
            servers:computed(()=>globalData.value.config.Client.Servers || []),
            relayServers:computed(()=>(globalData.value.config.Client.Relay || {Servers:[]}).Servers || []),
            holeServers:computed(()=>(globalData.value.config.Client.Tunnel || {Servers:[]}).Servers || []),

        });

        const handleConfig = ()=>{
            state.form.name = globalData.value.config.Client.Name;
            state.form.groupid = globalData.value.config.Client.GroupId;
            state.show = true;
        }
        const handleSave = ()=>{
            state.loading = true;
            updateConfigSet(state.form).then(()=>{
                state.loading = false;
                state.show = false;
                ElMessage.success('已操作');
                globalData.value.updateFlag = Date.now();
            }).catch((err)=>{
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }

        return {
            state,handleConfig,handleSave,
            
        }
    }
}
</script>
<style lang="stylus" scoped>
.status-server-wrap{
    padding-right:.5rem;
    a{color:#333;}
    span{border-radius:1rem;background-color:rgba(0,0,0,0.1);padding:0 .6rem; margin-left:.2rem}

    &.connected {
       a{color:green;font-weight:bold;}
       span{background-color:green;color:#fff;}
    }  
}

</style>