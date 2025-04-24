<template>
    <div v-if="config && hasExport" class="status-export-wrap">
        <a href="javascript:;" :title="$t('status.export')" @click="state.show = true">
            <el-icon size="16"><Share /></el-icon>
            {{$t('status.export')}}
        </a>
        <el-dialog class="options-center" :title="$t('status.export')" destroy-on-close v-model="state.show" center  width="580" top="1vh">
            <div class="port-wrap">
                <div class="text">
                    {{$t('status.exportText')}}
                </div>
                <div class="body">
                    <el-card shadow="never">
                        <template #header>
                            <div class="card-header">
                                <div class="flex">
                                    <div>
                                        <el-checkbox :disabled="onlyNode" v-model="state.single" :label="$t('status.exportSingle')" />
                                    </div>
                                    <div style="margin-left: 2rem;">
                                        <span>{{$t('status.exportName')}} : </span><el-input :disabled="!state.single" v-model="state.name" maxlength="32" show-word-limit style="width:15rem"></el-input>
                                    </div>
                                    <div>
                                        <span>{{$t('status.exportApiPassword')}} : </span><el-input type="password" show-password :disabled="onlyNode" v-model="state.apipassword" maxlength="36" show-word-limit style="width:15rem"></el-input>
                                    </div>
                                </div>
                                <div>
                                    <el-row>
                                        <el-col :span="8"><el-checkbox v-model="state.relay" :label="$t('status.exportRelay')" /></el-col>
                                        <el-col :span="8"><el-checkbox v-model="state.sforward" :label="$t('status.exportSForward')" /></el-col>
                                        <el-col :span="8"><el-checkbox v-model="state.updater" :label="$t('status.exportUpdater')" /></el-col>
                                    </el-row>
                                </div>
                                <div>
                                    <el-row>
                                        <el-col :span="8"><el-checkbox v-model="state.server" :label="$t('status.exportServer')" /></el-col>
                                        <el-col :span="8"><el-checkbox v-model="state.group" :label="$t('status.exportGroup')" /></el-col>
                                        <el-col :span="8"><el-checkbox v-model="state.tunnel" :label="$t('status.exportTunnel')" /></el-col>
                                    </el-row>
                                </div>
                            </div>
                        </template>
                        <Access ref="accessDom" :machineid="machineId"></Access>
                    </el-card>
                </div>
            </div>
            <template #footer>
                <el-button plain @click="state.show = false" :loading="state.loading">{{$t('common.cancel') }}</el-button>
                <el-button type="default" plain @click="handleExport" :loading="state.loading">{{$t('status.exportDownload') }}</el-button>
                <el-button type="info" plain @click="handleCopy" :loading="state.loading">{{$t('status.exportCopy') }}</el-button>
                <el-button type="success" plain @click="handleSave" :loading="state.loading">{{$t('status.exportSave') }}</el-button>
            </template>
        </el-dialog>
        <el-dialog class="options-center" :title="$t('status.export')" destroy-on-close v-model="state.showCopy" center  width="580" top="1vh">
            <div class="port-wrap">
                <el-input v-model="state.copyContent" type="textarea" :rows="10" resize="none" readonly></el-input>
            </div>
            <template #footer>
                <el-button plain @click="copyToClipboard">{{$t('status.exportCopy') }}</el-button>
            </template>
        </el-dialog>
        <el-dialog class="options-center" :title="$t('status.export')" destroy-on-close v-model="state.showSave" center  width="300" top="1vh">
            <div class="port-wrap">
                <div>
                    <el-input v-model="state.saveServer" readonly></el-input>
                </div>
                <div style="margin-top:1rem">
                    <el-input v-model="state.saveContent" readonly></el-input>
                </div>
            </div>
            <template #footer>
                <el-button plain @click="copySaveToClipboard">{{$t('status.exportCopy') }}</el-button>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import {  computed, reactive, ref } from 'vue';
import {Share} from '@element-plus/icons-vue'
import { exportConfig,copyConfig,saveConfig } from '@/apis/config';
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectGlobalData } from '@/provide';
import Access from '@/views/full/devices/Access.vue'
import { useI18n } from 'vue-i18n'
export default {
    components:{Share,Access},
    props:['config'],
    setup(props) {

        const { t } = useI18n();
        const globalData = injectGlobalData();
        const hasExport = computed(()=>globalData.value.hasAccess('Export')); 
        const onlyNode = computed(()=>globalData.value.config.Client.OnlyNode);
        const machineId = computed(()=>globalData.value.config.Client.Id);
        const state = reactive({
            show: false,
            loading:false,
            single:true,
            name:'',
            apipassword:onlyNode.value?  globalData.value.config.Client.CApi.ApiPassword :'',

            relay:true,
            sforward:true,
            updater:true,
            server:true,
            group:true,
            tunnel:true,

            copyContent:'',
            showCopy:false,

            saveServer:globalData.value.config.Client.Server.Host,
            saveContent:'',
            showSave:false
        });
        const accessDom = ref(null); 
      
        
        const getJson = ()=>{
            if(!hasExport.value){
                return;
            };
            const json = {
                access:accessDom.value.getValue(),
                single:state.single,
                name:state.name,
                apipassword:state.apipassword,
                relay:state.relay,
                sforward:state.sforward,
                updater:state.updater,
                server:state.server,
                group:state.group,
                tunnel:state.tunnel,
            }
            
            if(json.single){
                if(!json.name){
                    ElMessage.error(t('status.exportNamePlease'));
                    return;
                }
            }else{
                json.name = "";
            }
            if(!state.apipassword){
                ElMessage.error(t('status.exportApiPasswordPlease'));
                return;
            }
            return json;
        }
        const download = ()=>{
            const link = document.createElement('a');
            if(state.single){
                link.download = `client-node-export-${state.name}.zip`;
            }else{
                link.download = 'client-node-export.zip';
            }
            
            link.href = '/client-node-export.zip';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }

        const handleSave = ()=>{
            const json = getJson();
            if(!json){
                return;
            };
            state.loading = true;
            saveConfig(json).then((res)=>{
                state.loading = false;
                state.show = false;
                ElMessage.success(t('common.oper'));

                state.saveContent = res;
                state.showSave = true;

            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
                state.loading = false;
            });
        }
        const copySaveToClipboard = async ()=>{
            try {
                await navigator.clipboard.writeText(`在初始化linker客户端时，填写服务器和密钥，导入配置\n服务器: ${state.saveServer}\n密钥: ${state.saveContent}`);
                ElMessage.success(t('common.oper'));
                return true;
            } catch (err) {
                ElMessage.error(t('common.operFail'));
                return false;
            }
        }

        const handleExport = ()=>{
            
            const json = getJson();
            if(!json){
                return;
            };

            state.loading = true;
            exportConfig(json).then(()=>{
                state.loading = false;
                state.show = false;
                ElMessage.success(t('common.oper'));

                download();
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
                state.loading = false;
            });
        }
        const handleCopy = ()=>{
            const json = getJson();
            if(!json){
                return;
            };
            state.loading = true;
            copyConfig(json).then((res)=>{
                state.loading = false;
                state.show = false;
                ElMessage.success(t('common.oper'));
                state.copyContent = res;
                state.showCopy = true;
            }).catch(()=>{
                ElMessage.error(t('common.operFail'));
                state.loading = false;
            });
        }
        const copyToClipboard = async()=> {
            try {
                await navigator.clipboard.writeText(state.copyContent);
                ElMessage.success(t('common.oper'));
                return true;
            } catch (err) {
                ElMessage.error(t('common.operFail'));
                return false;
            }
        }

        return {config:props.config,onlyNode,hasExport,machineId, state,accessDom,handleSave,handleExport,handleCopy,copyToClipboard,copySaveToClipboard};
    }
}
</script>
<style lang="stylus" scoped>
.status-export-wrap{
    padding-right:2rem;
    a{color:#333;}
    .el-icon{
        vertical-align:text-top;
    }

    .el-col {text-align:left;}
}

</style>