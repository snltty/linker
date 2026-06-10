<template>
    <AccessBoolean value="UpdateServer">
        <template #default="{values}">
            <a href="javascript:;"  @click="handleUpdate(values)" class="download a-line" :title="updateText()" :class="updateColor()">
                <span>{{state.version}}</span>
                <template v-if="updaterServer.Version">
                    <template v-if="updaterServer.Status == 1">
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
        </template>
    </AccessBoolean>
    <el-dialog append-to=".app-wrap" class="options-center" :title="$t('updater')" destroy-on-close v-model="state.show" width="42rem" top="2vh">
        <div class="updater-wrap t-c">
            <div class="t-l">
                <ul>
                    <li v-for="item in state.msg">{{ item }}</li>
                </ul>
            </div>
            <div class="flex mgt-1 flex-center">
                <el-select v-model="state.versionValue" size="large" class="w-15" filterable allow-create default-first-option>
                    <el-option :key="updaterServer.Version" :label="updaterServer.Version" :value="updaterServer.Version" />
                </el-select>
            </div>
            <div class="mgt-2 t-c">
                <el-button type="success" @click="handleConfirm" plain>{{$t('common.confirm')}}</el-button>
            </div>
        </div>
    </el-dialog>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed, onMounted, reactive, ref } from 'vue';
import {Promotion,Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { confirmServer, exitServer,  getUpdaterMsg,  getUpdaterServer } from '@/apis/updater';
import { useI18n } from 'vue-i18n';
export default {
    components:{Promotion,Download,Loading,CircleCheck},
    props:['config'],
    setup(props) {

        const { t } = useI18n();
        const globalData = injectGlobalData();
        const updaterServer = ref({Version: '', Status: 0, Length: 0, Current: 0,Msg:[],DateTime:''});

        const state = reactive({
            show: false,
            msg:[],

            connected: computed(() => globalData.value.signin.Connected),
            version: computed(() => globalData.value.signin.Version),
            versionValue: '',
            timer:0
        });

        const _getUpdaterServer = ()=>{
            clearTimeout(state.timer);
            getUpdaterServer().then((res)=>{
                updaterServer.value.Version = res.Version;
                updaterServer.value.Status = res.Status;
                updaterServer.value.Length = res.Length;
                updaterServer.value.Current = res.Current;
                state.versionValue = res.Version;
                if(updaterServer.value.Status > 2 && updaterServer.value.Status < 6){
                    state.timer =  setTimeout(()=>{
                        _getUpdaterServer();
                    },1000);
                }
            }).catch(()=>{
                state.timer =  setTimeout(()=>{
                    _getUpdaterServer();
                },1000);
            });
        }
        const updateText = ()=>{
            if(!updaterServer.value.Version){
                return t('updater.no');
            }
            if(updaterServer.value.Status <= 2) {
                return state.version != updaterServer.value.Version  
                ? `${t('updater.notnew')}(${updaterServer.value.Version})` 
                : `${t('updater.new')}`
            }
            return {
                3:t('updater.downloading'),
                4:t('updater.downloaded'),
                5:t('updater.unziping'),
                6:t('updater.unzip'),
            }[updaterServer.value.Status];
        }
        const updateColor = ()=>{
            return state.version != updaterServer.value.Version  ? 'yellow' :'green'
        }
        const handleUpdate = (access)=>{
            if(!props.config || !access.UpdateServer){
                return;
            }
            //未检测，检测中，下载中，解压中
            if([0,1,3,5].indexOf(updaterServer.value.Status)>=0){
                ElMessage.error(t('common.operating'));
                return;
            }
            //已解压
            if(updaterServer.value.Status == 6){
                ElMessageBox.confirm(t('updater.close'), t('common.tips'), {
                    confirmButtonText: t('common.confirm'),
                    cancelButtonText:t('common.cancel'),
                    type: 'warning'
                }).then(() => {
                    exitServer();
                }).catch(() => {});
                return;
            }
 
            //已检测
            if(updaterServer.value.Status == 2){
                state.show = true;
            }
        }
        const handleConfirm = ()=>{
            state.show = false;
            confirmServer(state.versionValue || updaterServer.value.Version || globalData.value.signin.Version).then(()=>{
                setTimeout(()=>{
                    _getUpdaterServer();
                },1000);
            });
        }

        onMounted(() => {
            _getUpdaterServer();
            getUpdaterMsg().then((res)=>{
                state.msg = res.Msg;
            });
        });

        return {
         config:props.config,  state,updaterServer,handleUpdate,handleConfirm,updateText,updateColor
        }
    }
}
</script>
<style lang="stylus" scoped>
.updater-wrap{
    line-height:normal;
}
@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}

a{
    &.download{
        .el-icon{
            &.loading{
                animation:loading 1s linear infinite;
            }
            margin-left:.3rem;
            vertical-align: middle;
            margin-top: -4px;
        }
        span{display: inline-flex;align-items: center;line-height: 1;}
    }
}

</style>