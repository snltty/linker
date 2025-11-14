<template>
    <AccessBoolean value="UpdateSelf,UpdateOther">
        <template #default="{values}">
            <a href="javascript:;" class="download" @click="handleUpdate(values)" :title="updaterText" :class="updaterColor">
                <span>
                    <span>{{item.Version}}</span>
                    <template v-if="updater.list[item.MachineId]">
                        <template v-if="updater.list[item.MachineId].Status == 1">
                            <el-icon size="14" class="loading"><Loading /></el-icon>
                        </template>
                        <template v-else-if="updater.list[item.MachineId].Status == 2">
                            <el-icon size="14"><Download /></el-icon>
                        </template>
                        <template v-else-if="updater.list[item.MachineId].Status == 3 || updater.list[item.MachineId].Status == 5">
                            <el-icon size="14" class="loading"><Loading /></el-icon>
                            <span class="progress" v-if="updater.list[item.MachineId].Length ==0">0%</span>
                            <span class="progress" v-else>{{parseInt(updater.list[item.MachineId].Current/updater.list[item.MachineId].Length*100)}}%</span>
                        </template>
                        <template v-else-if="updater.list[item.MachineId].Status == 6">
                            <el-icon size="14" class="yellow"><CircleCheck /></el-icon>
                        </template>
                    </template>
                    <template v-else>
                        <el-icon size="14"><Download /></el-icon>
                    </template>
                </span>
            </a>
        </template>
    </AccessBoolean>   
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed} from 'vue';
import { ElMessage, ElMessageBox} from 'element-plus';
import {  exit } from '@/apis/updater';
import {Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { useUpdater } from './updater';
import { useI18n } from 'vue-i18n';

export default {
    props:['item','config'],
    components:{Download,Loading,CircleCheck},
    setup (props) {

         const {t} = useI18n();
        const globalData = injectGlobalData();
        const updater = useUpdater();
        const serverVersion = computed(()=>globalData.value.signin.Version);
        const updaterVersion = computed(()=>updater.value.current.Version);
        const updaterText = computed(()=>{
            if(!updater.value.list[props.item.MachineId]){
                return '未检测到更新';
            }
            
            if(updater.value.list[props.item.MachineId].Status <= 2) {
                return props.item.Version != serverVersion.value 
                ? `与服务器版本(${serverVersion.value})不一致，建议更新` 
                : updaterVersion.value != props.item.Version 
                    ? `不是最新版本(${updaterVersion.value})，建议更新` 
                    : `是最新版本，但我无法阻止你喜欢更新`
            }
            return {
                3:'正在下载',
                4:'已下载',
                5:'正在解压',
                6:'已解压，请重启',
            }[updater.value.list[props.item.MachineId].Status];
        })
        const updaterColor = computed(()=>{
            return props.item.Version != serverVersion.value 
            ? 'red' 
            : updater.value.list[props.item.MachineId] && updaterVersion.value != props.item.Version 
                ? 'yellow' :'green'
        })
        const handleUpdate = (access)=>{
            updater.value.device = props.item;
            if(!props.config){
                return;
            }
            if(!access.UpdateSelf){
                ElMessage.error('无权限');
                return;
            }
            if(props.item.MachineId != globalData.value.self.MachineId && !access.UpdateOther){
                ElMessage.error('无权限');
                return;
            }

            const updateInfo = updater.value.list[props.item.MachineId];
            if(!updateInfo){
                ElMessage.error('未检测到更新');
                return;
            }
            //未检测，检测中，下载中，解压中
            if([0,1,3,5].indexOf(updateInfo.Status)>=0){
                ElMessage.error('操作中，请稍后!');
                return;
            }
            //已解压
            if(updateInfo.Status == 6){
                ElMessageBox.confirm('确定关闭程序吗？', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    exit(props.item.MachineId);
                }).catch(() => {});
                return;
            }
            updater.value.show = updateInfo.Status == 2;
        }

       
        return {
            item:computed(()=>props.item),updater,updaterText,updaterColor,handleUpdate
        }
    }
}
</script>

<style lang="stylus" scoped>
@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}
a{
    color:#666;
    text-decoration: underline;
    &.green{color:green;font-weight:bold;}
}
a.download{
    margin-left:.6rem
    .el-icon{
        vertical-align:middle;font-weight:bold;
        &.loading{
            animation:loading 1s linear infinite;
        }

        margin-left:.3rem
    }
    +a.download{margin-left:.2rem}
}
</style>