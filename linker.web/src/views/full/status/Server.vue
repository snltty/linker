<template>
    <div class="status-server-wrap" :class="{ connected: state.connected }">
        <a href="javascript:;" title="更改你的连接设置" @click="handleConfig"> <el-icon size="16"><Promotion /></el-icon> 信标服务器</a>
        <a href="javascript:;" title="服务端的程序版本" @click="handleUpdate" class="download" :title="updateText()" :class="updateColor()">
            <span>{{state.version}}</span>
            <template v-if="updaterCurrent.Version">
                <template v-if="updaterCurrent.Status == 1">
                    <el-icon size="14" class="loading"><Loading /></el-icon>
                </template>
                <template v-else-if="updaterServer.Status == 2">
                    <el-icon size="14"><Download /></el-icon>
                </template>
                <template v-else-if="updaterServer.Status == 3 || updaterServer.Status == 5">
                    <el-icon size="14" class="loading"><Loading /></el-icon>
                    <span class="progress" v-if="updaterServer.Length ==0">0%</span>
                    <span class="progress" v-else>{{parseInt(updaterServer.Current/updaterServer.Length*100)}}%</span>
                </template>
                <template v-else-if="updaterServer.Status == 6">
                    <el-icon size="14" class="yellow"><CircleCheck /></el-icon>
                </template>
            </template>
            <template v-else>
                <el-icon size="14"><Download /></el-icon>
            </template>
        </a>
    </div>
    <el-dialog v-model="state.show" title="连接设置" width="300">
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="6rem">
                <el-form-item label="机器名" prop="name">
                    <el-input v-model="state.form.name" maxlength="12" show-word-limit />
                </el-form-item>
                <el-form-item label="分组名" prop="groupid">
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
import { setSignIn } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, onMounted, reactive, ref } from 'vue';
import {Promotion,Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { confirmServer, exitServer, getUpdaterCurrent, getUpdaterServer } from '@/apis/updater';
export default {
    components:{Promotion,Download,Loading,CircleCheck},
    props:['config'],
    setup(props) {

        const globalData = injectGlobalData();
        const hasConfig = computed(()=>globalData.value.hasAccess('Config')); 
        const hasUpdateServer = computed(()=>globalData.value.hasAccess('UpdateServer')); 
        const updaterCurrent = ref({Version: '', Msg: [], DateTime: '', Status: 0, Length: 0, Current: 0});
        const updaterServer = ref({Version: '', Status: 0, Length: 0, Current: 0});
        const updaterMsg = computed(()=>{
            return `${updaterCurrent.value.Version}->${updaterCurrent.value.DateTime}\n${updaterCurrent.value.Msg.map((value,index)=>`${index+1}、${value}`).join('\n')}`;
        });

        const state = reactive({
            show: false,
            loading: false,

            connected: computed(() => globalData.value.signin.Connected),
            version: computed(() => globalData.value.signin.Version),

            form: {
                name: globalData.value.config.Client.Name,
                groupid: globalData.value.config.Client.GroupId,
            },
            rules: {},
        });

        const _getUpdaterCurrent = ()=>{
            getUpdaterCurrent().then((res)=>{
                updaterCurrent.value.DateTime = res.DateTime;
                updaterCurrent.value.Version = res.Version;
                updaterCurrent.value.Status = res.Status;
                updaterCurrent.value.Length = res.Length;
                updaterCurrent.value.Current = res.Current;
                updaterCurrent.value.Msg = res.Msg;
                setTimeout(()=>{
                    _getUpdaterCurrent();
                },1000);
            }).catch(()=>{
                setTimeout(()=>{
                    _getUpdaterCurrent();
                },1000);
            })
        }
        const _getUpdaterServer = ()=>{
            getUpdaterServer().then((res)=>{
                updaterServer.value.Version = res.Version;
                updaterServer.value.Status = res.Status;
                updaterServer.value.Length = res.Length;
                updaterServer.value.Current = res.Current;
                if(updaterServer.value.Status > 2 && updaterServer.value.Status < 6){
                    setTimeout(()=>{
                        _getUpdaterServer();
                    },1000);
                }
            }).catch(()=>{
                setTimeout(()=>{
                    _getUpdaterServer();
                },1000);
            });
        }
        const updateText = ()=>{
            if(!updaterCurrent.value.Version){
                return '未检测到更新';
            }
            if(updaterServer.value.Status <= 2) {
                return state.version != updaterCurrent.value.Version  
                ? `不是最新版本(${updaterCurrent.value.Version})，建议更新\n${updaterMsg.value}` 
                : `是最新版本，但我无法阻止你喜欢更新\n${updaterMsg.value}`
            }
            return {
                3:'正在下载',
                4:'已下载',
                5:'正在解压',
                6:'已解压，请重启',
            }[updaterServer.value.Status];
        }
        const updateColor = ()=>{
            return state.version != updaterCurrent.value.Version  ? 'yellow' :'green'
        }
        const handleUpdate = ()=>{
            if(!props.config || !hasUpdateServer.value){
                return;
            }
            if(!updaterCurrent.value.Version){
                ElMessage.error('未检测到更新');
                return;
            }
            //未检测，检测中，下载中，解压中
            if([0,1,3,5].indexOf(updaterServer.value.Status)>=0){
                ElMessage.error('操作中，请稍后!');
                return;
            }
            //已解压
            if(updaterServer.value.Status == 6){
                ElMessageBox.confirm('确定关闭服务端吗？', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    exitServer();
                }).catch(() => {});
                return;
            }

            //已检测
            if(updaterCurrent.value.Status == 2){
                ElMessageBox.confirm('确定更新服务端吗？', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    confirmServer(updaterCurrent.value.Version).then(()=>{
                        setTimeout(()=>{
                            _getUpdaterServer();
                        },1000);
                    });
                }).catch(() => {});
            }
        }

        const handleConfig = () => {
            if(!props.config || !hasConfig.value){
                return;
            }
            state.form.name = globalData.value.config.Client.Name;
            state.form.groupid = globalData.value.config.Client.GroupId;
            state.show = true;
        }
        const handleSave = () => {
            state.loading = true;
            setSignIn(state.form).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success('已操作');
            }).catch((err) => {
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }

        onMounted(() => {
            _getUpdaterCurrent();
            _getUpdaterServer();
        });

        return {
         config:props.config,  state, handleConfig, handleSave,updaterCurrent,updaterServer,handleUpdate,updateText,updateColor
        }
    }
}
</script>
<style lang="stylus" scoped>

@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}

.status-server-wrap{
    padding-right:.5rem;
    a{color:#333;}
    a+a{margin-left:.6rem;}

    &.connected {
       a{color:green;font-weight:bold;}
    }  

    .el-icon{
        vertical-align:text-bottom;
    }

    a.download{
        .el-icon{
            font-weight:bold;
            &.loading{
                animation:loading 1s linear infinite;
            }
            margin-left:.3rem
        }
    }
}

</style>