<template>
    <div class="head-wrap">
        <div class="tools flex">
            <span class="label">服务器 </span><el-select v-model="state.server" placeholder="服务器" style="width:12rem" size="small">
                <el-option v-for="item in state.servers":key="item.Host" :label="item.Name":value="item.Host" ></el-option>
            </el-select>
            <span class="flex-1"></span>
            <el-button size="small" @click="handleEdit">
                编辑<el-icon><Edit /></el-icon>
            </el-button>
            <el-button size="small" @click="handleRefresh">
                刷新(F5)<el-icon><Refresh /></el-icon>
            </el-button>
        </div>
    </div>
    <el-dialog v-model="state.show" title="设置" width="260">
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="7rem">
                <el-form-item label="服务器" prop="host">
                    <el-input v-model="state.form.host"/>
                </el-form-item>
                <el-form-item label="中继密钥" prop="relaySecretKey">
                    <el-input v-model="state.form.relaySecretKey" type="password" show-password maxlength="36" show-word-limit />
                </el-form-item>
                <el-form-item label="分组号" prop="groupid">
                    <el-input v-model="state.form.groupid" type="password" show-password maxlength="36" show-word-limit />
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
import { injectGlobalData } from '@/provide';
import { reactive, watch } from 'vue';
import { Edit,Refresh } from '@element-plus/icons-vue';
import {save} from '@/apis/net'
import { ElMessage } from 'element-plus';
export default {
    components:{Edit,Refresh},
    setup () {
        const globalData = injectGlobalData();
        const state = reactive({
            server:"linker.snltty.com:1802",
            servers:[],
            groupid:'snltty',

            show:false,
            loading:false,
            form: {
                host: '',
                relaySecretKey: '',
                groupid: '',
            },
            rules: {},
        });
        watch(()=>globalData.value.config.Running.Client.Servers,()=>{
            state.servers = (globalData.value.config.Running.Client.Servers || []).slice(0,1);
            state.server = globalData.value.config.Client.Server;
            state.groupid = globalData.value.config.Client.GroupId;
        });

        const handleEdit = ()=>{
            state.form.host = state.server;
            state.form.groupid = state.groupid;
            state.form.relaySecretKey = globalData.value.config.Running.Relay.Servers.filter(c=>c.Host == state.form.host)[0].SecretKey;
            state.show = true;
        }
        const handleSave = ()=>{
            state.loading = true;
            save(state.form).then(()=>{
                state.loading = false;
                state.show = false;
                ElMessage.success('操作成功!');
            }).catch(()=>{
                state.loading =false;
                ElMessage.error('操作失败!');
            })
        }

        const handleRefresh = ()=>{
            window.location.reload();
        }

        return {
            state,handleRefresh,handleEdit,handleSave
        }
    }
}
</script>

<style lang="stylus" scoped>
.head-wrap{
    background-color:#fff;
    padding:1rem;
    border-bottom:1px solid #ddd;
    box-shadow:1px 2px 3px rgba(0,0,0,.05);

    font-size:1.4rem;

    span.label{
        line-height:2.4rem
        margin-right:.6rem
        color:#555;
    }
}
</style>