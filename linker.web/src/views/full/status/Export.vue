<template>
    <div v-if="config && hasExport" class="status-export-wrap">
        <a href="javascript:;" title="此设备的管理接口" @click="state.show = true">
            <el-icon size="16"><Share /></el-icon>
            导出配置
        </a>
        <el-dialog class="options-center" title="导出配置" destroy-on-close v-model="state.show" center  width="580" top="1vh">
            <div class="port-wrap">
                <div class="text">
                    导出配置，作为子设备运行，如果使用docker，容器映射configs文件夹即可
                </div>
                <div class="body">
                    <el-card shadow="never">
                        <template #header>
                            <div class="card-header">
                                <div class="flex" style="margin-bottom: 1rem;">
                                    <div>
                                        <el-popover placement="top-start" title="tips":width="200" trigger="hover"
                                            content="这将生成唯一ID，多台设备使用产生冲突，挤压下线">
                                            <template #reference>
                                                <el-checkbox :disabled="onlyNode" v-model="state.single" label="单设备" />
                                            </template>
                                        </el-popover>
                                    </div>
                                    <div style="margin-left: 2rem;">
                                        <span>设备名 : </span><el-input :disabled="!state.single" v-model="state.name" maxlength="12" show-word-limit style="width:15rem"></el-input>
                                    </div>
                                    <div>
                                        <span>管理密码 : </span><el-input type="password" show-password :disabled="onlyNode" v-model="state.apipassword" maxlength="36" show-word-limit style="width:15rem"></el-input>
                                    </div>
                                </div>
                                <div class="flex">
                                    <div>
                                        <span>Action参数(自定义验证) : </span><el-input v-model="state.nodeArg" style="width:34.2rem"></el-input>
                                    </div>
                                </div>
                            </div>
                        </template>
                        <Access ref="accessDom" :machineid="machineId"></Access>
                    </el-card>
                    
                </div>
            </div>
            <template #footer>
                <el-button plain @click="state.show = false" :loading="state.loading">取消</el-button>
                <el-button type="success" plain @click="handleExport" :loading="state.loading">确定导出</el-button>
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
export default {
    components:{Share,Access},
    props:['config'],
    setup(props) {

        const globalData = injectGlobalData();
        const hasExport = computed(()=>globalData.value.hasAccess('Export')); 
        const onlyNode = computed(()=>globalData.value.config.Client.OnlyNode);
        const machineId = computed(()=>globalData.value.config.Client.Id);
        const state = reactive({
            show: false,
            loading:false,
            single:true,
            name:'',
            nodeArg:globalData.value.config.Client.ServerInfo.Arg || '',
            apipassword: globalData.value.config.Client.CApi.ApiPassword,
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
                ActionArg:state.nodeArg,
                apipassword:state.apipassword
            }
            if(json.nodeArg){
                try{
                    if(typeof(JSON.parse(json.nodeArg)) != 'object'){
                        ElMessage.error('Action参数错误，需要JSON格式');
                        return;
                    }
                }catch(e){
                    ElMessage.error('Action参数错误，需要JSON格式');
                    return;
                }
            }
            
            if(json.single){
                if(!json.name){
                    ElMessage.error('请输入设备名');
                    return;
                }
            }else{
                json.name = "";
            }
            if(json.single && !state.name){
                ElMessage.error('请输入管理密码');
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
                ElMessage.success('导出成功');

                download();
            }).catch(()=>{
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