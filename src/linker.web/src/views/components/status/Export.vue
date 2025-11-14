<template>
    <AccessShow value="Export">
        <div v-if="config" class="status-export-wrap">
            <a href="javascript:;" :title="$t('status.export')" @click="state.show = true">
                <el-icon size="16"><Share /></el-icon>
                <PcShow>
                    <span>{{$t('status.export')}}</span>
                </PcShow>
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
                                    <div>
                                        <el-row>
                                            <el-col :span="12"><el-checkbox :disabled="onlyNode" v-model="state.single" :label="$t('status.exportSingle')" /></el-col>
                                            <el-col :span="12">
                                                <div class="flex flex-nowrap">
                                                    <span style="width: 11rem;">{{$t('status.exportName')}} : </span><el-input v-trim :disabled="!state.single" v-model="state.name" maxlength="32" show-word-limit></el-input>
                                                </div>
                                            </el-col>
                                        </el-row>
                                    </div>
                                    <div>
                                        <el-row>
                                            <el-col :span="12">
                                                <div class="flex flex-nowrap mgt-1">
                                                    <span style="width: 11rem;">{{$t('status.exportWebport')}} : </span><el-input v-trim :disabled="onlyNode" v-model="state.webport"></el-input>
                                                </div>
                                            </el-col>
                                            <el-col :span="12">
                                                <div class="flex flex-nowrap mgt-1">
                                                    <span style="width: 11rem;">{{$t('status.exportApiPassword')}} : </span><el-input v-trim type="password" show-password :disabled="onlyNode" v-model="state.apipassword" maxlength="36" show-word-limit></el-input>
                                                </div>
                                            </el-col>
                                        </el-row>
                                    </div>
                                    <div>
                                        <el-row>
                                            <el-col :xs="12" :sm="8"><el-checkbox v-model="state.relay" :label="$t('status.exportRelay')" /></el-col>
                                            <el-col :xs="12" :sm="8"><el-checkbox v-model="state.updater" :label="$t('status.exportUpdater')" /></el-col>
                                            <el-col :xs="12" :sm="8"><el-checkbox v-model="state.group" :label="$t('status.exportGroup')" /></el-col>
                                            <el-col :xs="12" :sm="8"><el-checkbox v-model="state.server" :label="$t('status.exportServer')" /></el-col>
                                            <el-col :xs="12" :sm="8"><el-checkbox v-model="state.super" :label="$t('status.exportSuper')" /></el-col>
                                            <!-- <el-col :xs="12" :sm="8"><el-checkbox v-model="state.tunnel" :label="$t('status.exportTunnel')" /></el-col> -->
                                            
                                        </el-row>
                                    </div>
                                </div>
                            </template>
                            <Access ref="accessDom" :machineid="machineId" :height="30"></Access>
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
                    <el-input v-trim v-model="state.copyContent" type="textarea" :rows="10" resize="none" readonly></el-input>
                </div>
                <template #footer>
                    <el-button plain @click="copyToClipboard">{{$t('status.exportCopy') }}</el-button>
                </template>
            </el-dialog>
            <el-dialog class="options-center" :title="$t('status.export')" destroy-on-close v-model="state.showSave" center  width="300" top="1vh">
                <div class="port-wrap">
                    <div>
                        <el-input v-trim v-model="state.saveServer" readonly></el-input>
                    </div>
                    <div style="margin-top:1rem">
                        <el-input v-trim v-model="state.saveContent" readonly></el-input>
                    </div>
                </div>
                <template #footer>
                    <el-button plain @click="copySaveToClipboard">{{$t('status.exportCopy') }}</el-button>
                </template>
            </el-dialog>
        </div>
    </AccessShow>
</template>
<script>
import {  computed, reactive, ref } from 'vue';
import {Share} from '@element-plus/icons-vue'
import { exportConfig,copyConfig,saveConfig } from '@/apis/config';
import { ElMessage } from 'element-plus';
import { injectGlobalData } from '@/provide';
import Access from '../accesss/Access.vue'
import { useI18n } from 'vue-i18n'
export default {
    components:{Share,Access},
    props:['config'],
    setup(props) {

        const { t } = useI18n();
        const globalData = injectGlobalData();
        const onlyNode = computed(()=>globalData.value.config.Client.OnlyNode);
        const machineId = computed(()=>globalData.value.config.Client.Id);
        const state = reactive({
            show: false,
            loading:false,
            single:true,
            name:'',
            apipassword:onlyNode.value? globalData.value.config.Client.CApi.ApiPassword :'',
            webport: globalData.value.config.Client.CApi.WebPort,

            relay:true,
            updater:true,
            server:true,
            super:false,
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
            const access = accessDom.value.getValue();
            const json = {
                access:access[0],
                fullAccess:access[1],
                single:state.single,
                name:state.name,
                apipassword:state.apipassword,
                webport:+state.webport,
                relay:state.relay,
                updater:state.updater,
                server:state.server,
                super:state.super,
                group:state.group,
                tunnel:state.tunnel
            }
            
            if(json.single){
                if(!json.name){
                    ElMessage.error(t('status.exportNamePlease'));
                    return;
                }
            }else{
                json.name = "";
            }
            if(!json.apipassword){
                ElMessage.error(t('status.exportApiPasswordPlease'));
                return;
            }
            if(!json.webport || isNaN(json.webport) || json.webport<=0 || json.webport>65535){
                ElMessage.error(t('status.exportWebportPlease'));
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
                if(res){
                    state.show = false;
                    ElMessage.success(t('common.oper'));

                    state.saveContent = res;
                    state.showSave = true;
                }else{
                    ElMessage.error(t('common.operFail'));
                }
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

        return {globalData,config:props.config,onlyNode,machineId, state,accessDom,
            handleSave,handleExport,handleCopy,copyToClipboard,copySaveToClipboard};
    }
}
</script>
<style lang="stylus" scoped>
html.dark .status-wrap .status-export-wrap  a{color:#ccc;}
.status-export-wrap{
    padding-right:1rem;
    a{
        color:#333;
        .el-icon{
            vertical-align: sub;
        }
    }
    .el-col {text-align:left;}
}

</style>