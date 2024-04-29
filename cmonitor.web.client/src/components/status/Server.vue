<template>
    <div class="status-server-wrap" :class="{connected:connected}">
        <a href="javascript:;" @click="handleConfig">服务器 {{server}}</a>
        <span class="num">{{serverLength}}</span>
    </div>
    <el-dialog v-model="state.show" title="登入设置" width="500">
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
                <el-form-item label=""  label-width="0">
                    <el-col :span="12">
                        <el-form-item label="服务器" prop="server">
                            <el-select v-model="state.form.server">
                                <template v-for="(item,index) in servers" :key="index">
                                    <el-option :label="item.Name" :value="item.Host" >
                                        <div class="flex">
                                            <span>【{{item.Name}}】</span>
                                            <span>{{item.Host}}</span>
                                            <span class="pdl-6" @click.stop>
                                                <el-popconfirm title="删除不可逆，确认吗?" @confirm.stop="handleDel(item)">
                                                    <template #reference>
                                                        <el-button size="small">删除</el-button>
                                                    </template>
                                                </el-popconfirm>
                                                
                                            </span>
                                        </div>
                                    </el-option>
                                </template>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <div class="pdl-6"><el-button @click="handleAdd">添加服务器</el-button></div>
                    </el-col>
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

    <el-dialog v-model="state.showAdd" title="添加服务器" width="300">
        <div>
            <el-form :model="state.formAdd" :rules="state.rulesAdd" label-width="6rem">
                <el-form-item label="名称" prop="name">
                    <el-input v-model="state.formAdd.name" maxlength="12" show-word-limit />
                </el-form-item>
                <el-form-item label="地址" prop="host">
                    <el-input v-model="state.formAdd.host" placeholder="ip/域名:端口" />
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
        <div class="dialog-footer t-c">
            <el-button @click="state.showAdd = false" :loading="state.loading">取消</el-button>
            <el-button type="primary" @click="handleSaveAdd" :loading="state.loading">确定保存</el-button>
        </div>
        </template>
    </el-dialog>
</template>
<script>
import { updateConfigSet, updateConfigSetServers } from '@/apis/tunnel';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue';

export default {
    setup(props) {
        
        const globalData = injectGlobalData();
        const connected = computed(()=>globalData.value.signin.Connected);
        const connecting = computed(()=>globalData.value.signin.Connecting);
        const server = computed(()=>globalData.value.config.Client.Server);
        const servers = computed(()=>globalData.value.config.Client.Servers || []);
        const serverLength = computed(()=>(globalData.value.config.Client.Servers||[]).length);

        const state = reactive({
            show:false,
            loading:false,
            form:{
                name:globalData.value.config.Client.Name,
                server:globalData.value.config.Client.Server,
                groupid:globalData.value.config.Client.GroupId,
            },
            rules:{},

            showAdd:false,
            formAdd:{
                name:'',
                host:''
            },
            rulesAdd:{
                name:[
                    { required: true, message: '听填写', trigger: 'blur' },
                ],
                host:[
                    { required: true, message: '听填写', trigger: 'blur' },
                ]
            },
        });
        const handleConfig = ()=>{
            state.form.name = globalData.value.config.Client.Name;
            state.form.server = globalData.value.config.Client.Server;
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
        
        const handleDel = (item)=>{
            const servers = (globalData.value.config.Client.Servers || []).filter(c=>c.Host != item.Host || c.Name != item.Name);
            state.loading = true;
            updateConfigSetServers(servers).then(()=>{
                state.loading = false;
                ElMessage.success('已操作');
                globalData.value.updateFlag = Date.now();
            }).catch((err)=>{
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }
        const handleAdd = ()=>{
            state.showAdd = true;
            state.formAdd.name = '';
            state.formAdd.host = '';
        }
        const handleSaveAdd = ()=>{
            const servers = globalData.value.config.Client.Servers || [];

            const name  =  state.formAdd.name.replace(/^\s|\s$/g,'');
            const host  =  state.formAdd.host.replace(/^\s|\s$/g,'');
            if(servers.filter(c=>c.Host == host).length > 0 || servers.filter(c=>c.Name == name).length > 0){
                ElMessage.error('已存在差不多相同的记录!');                
                return;
            }
            servers.push({Name:name,Host:host});

            state.loading = true;
            updateConfigSetServers(servers).then(()=>{
                state.loading = false;
                state.showAdd = false;
                ElMessage.success('已操作');
                globalData.value.updateFlag = Date.now();
            }).catch((err)=>{
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }

        return {
            connected,connecting,server,servers,serverLength,state,handleConfig,handleSave,
            handleDel,handleAdd,handleSaveAdd
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