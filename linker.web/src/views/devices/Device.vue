<template>
<el-table-column prop="MachineId" label="设备">
    <template #header>
        <div class="flex">
            <span class="flex-1">设备</span>
            <span> <el-input size="small" v-model="name" clearable @input="handleRefresh" placeholder="设备/虚拟网卡/端口转发" ></el-input> </span>
            <span>
                <el-button size="small" @click="handleRefresh"><el-icon><Search /></el-icon></el-button>
            </span>
        </div>
    </template>
    <template #default="scope">
        <div>
            <p>
                <a href="javascript:;" @click="handleEdit(scope.row)" :class="{green:scope.row.Connected}">{{scope.row.MachineName }}</a>
                <strong v-if="scope.row.isSelf"> - (<el-icon><StarFilled /></el-icon> 本机) </strong>
            </p>
            <p class="flex">
                <span>{{ scope.row.IP }}</span>
                <span class="flex-1"></span>
                <a href="javascript:;" class="download" @click="handleUpdate(scope.row)" :title="updateText(scope.row)" :class="updateColor(scope.row)">
                    <span>
                        <span>{{scope.row.Version}}</span>
                        <template v-if="updater.list[scope.row.MachineId]">
                            <template v-if="updater.list[scope.row.MachineId].Status == 1">
                                <el-icon size="14" class="loading"><Loading /></el-icon>
                            </template>
                            <template v-else-if="updater.list[scope.row.MachineId].Status == 2">
                                <el-icon size="14"><Download /></el-icon>
                            </template>
                            <template v-else-if="updater.list[scope.row.MachineId].Status == 3 || updater.list[scope.row.MachineId].Status == 5">
                                <el-icon size="14" class="loading"><Loading /></el-icon>
                                <span class="progress" v-if="updater.list[scope.row.MachineId].Length ==0">0%</span>
                                <span class="progress" v-else>{{parseInt(updater.list[scope.row.MachineId].Current/updater.list[scope.row.MachineId].Length*100)}}%</span>
                            </template>
                            <template v-else-if="updater.list[scope.row.MachineId].Status == 6">
                                <el-icon size="14" class="yellow"><CircleCheck /></el-icon>
                            </template>
                        </template>
                        <template v-else>
                            <el-icon size="14"><Download /></el-icon>
                        </template>
                    </span>
                </a>
            </p>
        </div>
    </template>
</el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { computed, ref,h } from 'vue';
import {StarFilled,Search,Download,Loading,CircleCheck} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox,ElSelect,ElOption } from 'element-plus';
import { confirm, exit } from '@/apis/updater';
import { useUpdater } from './updater';

export default {
    emits:['edit','refresh'],
    components:{StarFilled,Search,Download,Loading,CircleCheck},
    setup(props,{emit}) {

        const name = ref(sessionStorage.getItem('search-name') || '');
        const globalData = injectGlobalData();
        const updater = useUpdater();
        const serverVersion = computed(()=>globalData.value.signin.Version);
        const updaterVersion = computed(()=>updater.value.current.Version);
        const updaterMsg = computed(()=>{
            return `${updaterVersion.value}->${updater.value.current.DateTime}\n${updater.value.current.Msg.map((value,index)=>`${index+1}、${value}`).join('\n')}`;
        });
        
        const updateText = (row)=>{
            if(!updater.value.list[row.MachineId]){
                return '未检测到更新';
            }
            
            if(updater.value.list[row.MachineId].Status <= 2) {
                return row.Version != serverVersion.value 
                ? `与服务器版本(${serverVersion.value})不一致，建议更新` 
                : updaterVersion.value != row.Version 
                    ? `不是最新版本(${updaterVersion.value})，建议更新\n${updaterMsg.value}` 
                    : `是最新版本，但我无法阻止你喜欢更新\n${updaterMsg.value}`
            }
            return {
                3:'正在下载',
                4:'已下载',
                5:'正在解压',
                6:'已解压，请重启',
            }[updater.value.list[row.MachineId].Status];
        }
        const updateColor = (row)=>{
            return row.Version != serverVersion.value 
            ? 'red' 
            : updater.value.list[row.MachineId] && updaterVersion.value != row.Version 
                ? 'yellow' :'green'
        }

        const handleEdit = (row)=>{
            emit('edit',row)
        }
        const handleRefresh = ()=>{
            sessionStorage.setItem('search-name',name.value);
            emit('refresh',name.value)
        }

        const handleUpdate = (row)=>{
            const updateInfo = updater.value.list[row.MachineId];
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
                    exit(row.MachineId);
                }).catch(() => {});
                return;
            }

            //已检测
            if(updateInfo.Status == 2){

                const selectedValue = ref(updaterVersion.value);
                const selectOptions = [
                    h(ElOption, { label: `仅[${row.MachineName}] -> ${updaterVersion.value}(最新版本)`, value: updaterVersion.value }),
                    h(ElOption, { label: `[所有] -> ${updaterVersion.value}(最新版本)`, value: `all->${updaterVersion.value}` }),
                ];
                if(row.Version != serverVersion.value && updaterVersion.value != serverVersion.value){
                    selectOptions.push(h(ElOption, { label: `仅[${row.MachineName}] -> ${serverVersion.value}(服务器版本)`, value: serverVersion.value }));
                    selectOptions.push(h(ElOption, { label: `[所有] -> ${serverVersion.value}(服务器版本)`, value: `all->${serverVersion.value}` }));
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
                        MachineId:row.MachineId,
                        Version:selectedValue.value.replace('all->',''),
                        All:selectedValue.value.indexOf('all->') >= 0
                    };
                    if(data.All){
                        data.MachineId = '';
                    }
                    confirm(data);
                }).catch(() => {});
            }
        }

        return {
             handleEdit,handleRefresh,name,updater,updateText,updateColor,handleUpdate
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

.el-input{
    width:15rem;
    margin-right:.6rem
}
</style>