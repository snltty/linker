<template>
    <el-table-column label="操作"  fixed="right" width="75">
        <template #default="scope">
            <el-dropdown size="small">
                <div class="dropdown">
                    <span>操作</span>
                    <el-icon class="el-icon--right">
                        <ArrowDown />
                    </el-icon>
                </div>
                <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item v-if="scope.row.showReboot && hasReboot" @click="handleExit(scope.row.MachineId,scope.row.MachineName)"><el-icon><SwitchButton /></el-icon> 重启</el-dropdown-item>
                        <el-dropdown-item v-if="scope.row.showDel && hasRemove" @click="handleDel(scope.row.MachineId,scope.row.MachineName)"><el-icon><Delete /></el-icon> 删除</el-dropdown-item>
                        <el-dropdown-item v-if="handleShowAccess(scope.row,accessList[scope.row.MachineId] || '0')" @click="handleAccess(scope.row)"><el-icon><Flag /></el-icon> 权限</el-dropdown-item>
                        <el-dropdown-item v-if="scope.row.isSelf && hasApiPassword" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon> 管理接口</el-dropdown-item>
                        <el-dropdown-item v-else-if="hasApiPasswordOther" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon> 管理接口</el-dropdown-item>
                        <el-dropdown-item @click="handleStopwatch(scope.row.MachineId,scope.row.MachineName)"><el-icon><Platform /></el-icon>它的信标</el-dropdown-item>
                        <el-dropdown-item @click="handleStopwatch('',$t('status.messenger'))"><el-icon><Platform /></el-icon>服务器信标</el-dropdown-item>
                        <el-dropdown-item @click="handleRoutes(scope.row.MachineId,scope.row.MachineName)"><el-icon><Paperclip /></el-icon>网卡路由</el-dropdown-item>
                        
                        <el-dropdown-item v-if="scope.row.isSelf && hasFirewallSelf" @click="handleFirewall(scope.row.MachineId,scope.row.MachineName)"><el-icon><CircleCheck /></el-icon>防火墙</el-dropdown-item>
                        <el-dropdown-item v-else-if="hasFirewallOther" @click="handleFirewall(scope.row.MachineId,scope.row.MachineName)"><el-icon><CircleCheck /></el-icon>防火墙</el-dropdown-item>

                        <el-dropdown-item v-if="scope.row.isSelf && hasWakeupSelf" @click="handleWakeup(scope.row.MachineId,scope.row.MachineName)"><el-icon><VideoPlay /></el-icon>唤醒</el-dropdown-item>
                        <el-dropdown-item v-else-if="hasWakeupOther" @click="handleWakeup(scope.row.MachineId,scope.row.MachineName)"><el-icon><VideoPlay /></el-icon>唤醒</el-dropdown-item>
                    </el-dropdown-menu>
                </template>
            </el-dropdown>
            
        </template>
    </el-table-column>
</template>

<script>
import { signInDel } from '@/apis/signin';
import { exit } from '@/apis/updater';
import { injectGlobalData } from '@/provide';
import { Delete,SwitchButton,ArrowDown, Flag,HelpFilled,Platform,Paperclip,CircleCheck,VideoPlay } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed } from 'vue';
import { useAccess } from './access';
import { setApiPassword } from '@/apis/access';
import { useFlow } from './flow';
import { useTuntap } from './tuntap';
import { useOper } from './oper';

export default {
    emits:['refresh','access'],
    components:{Delete,SwitchButton,ArrowDown,Flag,HelpFilled,Platform,Paperclip,CircleCheck,VideoPlay},
    setup (props,{emit}) {
        
        const globalData = injectGlobalData();

        const allAccess = useAccess();
        const myAccess = computed(()=>globalData.value.config.Client.AccessBits);
        const hasAccess = computed(()=>globalData.value.hasAccess('Access')); 
        const accessList = computed(()=>allAccess.value.list);
        
        const hasReboot = computed(()=>globalData.value.hasAccess('Reboot')); 
        const hasRemove = computed(()=>globalData.value.hasAccess('Remove')); 

        const hasApiPassword = computed(()=>globalData.value.hasAccess('SetApiPassword')); 
        const hasApiPasswordOther = computed(()=>globalData.value.hasAccess('SetApiPasswordOther')); 

        const hasFirewallSelf = computed(()=>globalData.value.hasAccess('FirewallSelf')); 
        const hasFirewallOther = computed(()=>globalData.value.hasAccess('FirewallOther')); 

        const hasWakeupSelf = computed(()=>globalData.value.hasAccess('WakeupSelf')); 
        const hasWakeupOther = computed(()=>globalData.value.hasAccess('WakeupOther')); 
        
        const flow = useFlow();
        const oper  = useOper();
        

        const handleDel = (machineId,machineName)=>{
            ElMessageBox.confirm(`确认删除[${machineName}]?`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                signInDel(machineId).then(()=>{
                    emit('refresh');
                });
            }).catch(() => {});
        }
        const handleExit = (machineId,machineName)=>{
            ElMessageBox.confirm(`确认关闭[${machineName}]?`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                exit(machineId).then(()=>{
                    emit('refresh');
                })
            }).catch(() => {});
        }


        const json = {'0':1,'1':0};
        const handleShowAccess = (row,rowAccess)=>{ 
            let maxLength = Math.max(myAccess.value.length,rowAccess.length);
            let myValue = myAccess.value.padEnd(maxLength,'0').split('').map(c=>json[c]);
            let rowValue = rowAccess.padEnd(maxLength,'0').split('');
            return row.showAccess  && hasAccess.value  && rowAccess >= 0 && myValue.map((v,i)=>{
                return v == rowValue[i] && v == '1';
            }).filter(c=>c).length > 0;
        }
        const handleAccess = (row)=>{
            emit('access',row);
        }

        const handleApiPassword = (row)=>{
            ElMessageBox.prompt('输入新的管理接口密码', `重置【${row.MachineName}】的接口密码`, {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputPattern:/^[0-9a-zA-Z]{1,32}$/,
                inputErrorMessage: '数字字母1-32位',
            }).then(({ value }) => {
                setApiPassword({machineId:row.MachineId,password:value}).then(()=>{
                    ElMessage.success('操作成功，重启后生效~');
                }).catch(()=>{
                    ElMessage.error('操作失败~');
                })
            }).catch(() => {
            })
        }

        const handleStopwatch = (id,name)=>{
            flow.value.device.id = id;
            flow.value.device.name = name;
            flow.value.show = true;
        }
        const handleRoutes = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showRoutes = true;
        }
        const handleFirewall = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showFirewall = true;
        }
        const handleWakeup = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showWakeup = true;
        }

        return {accessList,handleDel,handleExit,hasReboot,hasRemove,hasAccess,handleShowAccess,handleAccess,
            hasApiPassword,hasApiPasswordOther,handleApiPassword,handleStopwatch,handleRoutes,
            hasFirewallSelf,hasFirewallOther,handleFirewall,hasWakeupSelf,hasWakeupOther,handleWakeup
        }
    }
}
</script>

<style lang="stylus" scoped>
.dropdown{
    border:1px solid #ddd;
    padding:.4rem;
    font-size:1.3rem;
    border-radius:.4rem;
    position:relative;
    .el-icon{
        vertical-align:middle;
    }

    .badge{
        position:absolute;
        right:-1rem;
        top:-50%;
        border-radius:10px;
        background-color:#f1ae05;
        color:#fff;
        padding:.2rem .6rem;
        font-size:1.2rem;
        
    }
}
</style>