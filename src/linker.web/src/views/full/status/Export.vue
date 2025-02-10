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
                            </div>
                        </template>
                        <Access ref="accessDom" :machineid="machineId"></Access>
                    </el-card>
                    
                </div>
            </div>
            <template #footer>
                <el-button plain @click="state.show = false" :loading="state.loading">{{$t('common.cancel') }}</el-button>
                <el-button type="success" plain @click="handleExport" :loading="state.loading">{{$t('common.confirm') }}</el-button>
            </template>
        </el-dialog>
    </div>
</template>
<script>
import {  computed, onMounted, reactive, ref } from 'vue';
import {Share} from '@element-plus/icons-vue'
import { exportConfig } from '@/apis/config';
import { ElMessage } from 'element-plus';
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
            apipassword:'',
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
                apipassword:state.apipassword
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
        return {config:props.config,onlyNode,hasExport,machineId, state,accessDom,handleExport};
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