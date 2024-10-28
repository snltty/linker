<template>
    <a href="javascript:;" class="download" @click="handleUpdate()" :title="updaterText" :class="updaterColor">
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

<script>
import { injectGlobalData } from '@/provide';
import { computed, h, ref } from 'vue';
import { ElMessage, ElMessageBox, ElOption, ElSelect } from 'element-plus';
import { confirm } from '@/apis/updater';
import {Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { useUpdater } from './updater';

export default {
    props:['item','config'],
    components:{Download,Loading,CircleCheck},
    setup (props) {

        const globalData = injectGlobalData();
        const hasUpdateSelf = computed(()=>globalData.value.hasAccess('UpdateSelf')); 
        const hasUpdateOther = computed(()=>globalData.value.hasAccess('UpdateOther')); 
        const updater = useUpdater();
        const serverVersion = computed(()=>globalData.value.signin.Version);
        const updaterVersion = computed(()=>updater.value.current.Version);
        const updaterMsg = computed(()=>{
            return `${updaterVersion.value}->${updater.value.current.DateTime}\n${updater.value.current.Msg.map((value,index)=>`${index+1}、${value}`).join('\n')}`;
        });
        const updaterText = computed(()=>{
            if(!updater.value.list[props.item.MachineId]){
                return '未检测到更新';
            }
            
            if(updater.value.list[props.item.MachineId].Status <= 2) {
                return props.item.Version != serverVersion.value 
                ? `与服务器版本(${serverVersion.value})不一致，建议更新` 
                : updaterVersion.value != props.item.Version 
                    ? `不是最新版本(${updaterVersion.value})，建议更新\n${updaterMsg.value}` 
                    : `是最新版本，但我无法阻止你喜欢更新\n${updaterMsg.value}`
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
        const handleUpdate = ()=>{
            if(!props.config){
                return;
            }
            if(!hasUpdateSelf.value){
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

            //已检测
            if(updateInfo.Status == 2){

                const selectedValue = ref(updaterVersion.value);
                const selectOptions = [
                    h(ElOption, { label: `仅[${props.item.MachineName}] -> ${updaterVersion.value}(最新)`, value: updaterVersion.value }),
                ];
                if(props.config && hasUpdateOther.value){
                    selectOptions.push(h(ElOption, { label: `[本组所有] -> ${updaterVersion.value}(最新)`, value: `allg->${updaterVersion.value}` }));
                    selectOptions.push(h(ElOption, { label: `[本服务器所有] -> ${updaterVersion.value}(最新)(需要密钥)`, value: `all->${updaterVersion.value}` }));
                }
                if(props.item.Version != serverVersion.value && updaterVersion.value != serverVersion.value){
                    selectOptions.push(h(ElOption, { label: `仅[${props.item.MachineName}] -> ${serverVersion.value}(服务器版本)`, value: serverVersion.value }));
                    if(props.config && hasUpdateOther.value){
                        selectOptions.push(h(ElOption, { label: `[本组所有] -> ${serverVersion.value}(服务器版本)`, value: `allg->${serverVersion.value}` }));
                        selectOptions.push(h(ElOption, { label: `[本服务器所有] -> ${serverVersion.value}(服务器版本)(需要密钥)`, value: `all->${serverVersion.value}` }));
                    }
                }

                ElMessageBox({
                    title: '选择版本',
                    message: () => h(ElSelect, {
                        modelValue: selectedValue.value,
                        placeholder: '请选择',
                        style:'width:20rem;',
                        'onUpdate:modelValue': (val) => {
                            selectedValue.value = val
                        }
                    }, selectOptions),
                    confirmButtonText: '确定',
                    cancelButtonText: '取消'
                }).then(() => {
                    const data = {
                        MachineId:props.item.MachineId,
                        Version:selectedValue.value.replace('all->','').replace('allg->',''),
                        GroupAll:selectedValue.value.indexOf('allg->') >= 0,
                        All:selectedValue.value.indexOf('all->') >= 0,
                    };
                    if(data.All || data.GroupAll){
                        data.MachineId = '';
                    }
                    confirm(data);
                }).catch(() => {});
            }
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
}
</style>