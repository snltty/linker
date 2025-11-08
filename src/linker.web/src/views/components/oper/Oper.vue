<template>
    <AccessBoolean value="Access">
        <template #default="{values}">
            <el-table-column :label="$t('home.oper')"  fixed="right" width="75">
                <template #default="scope">
                    <el-dropdown size="small" >
                        <div class="dropdown">
                            <span>{{$t('home.oper')}}</span>
                            <el-icon class="el-icon--right">
                                <ArrowDown />
                            </el-icon>
                        </div>
                        <template #dropdown>
                            <el-dropdown-menu>
                                <template v-if="scope.row.Connected">
                                    <AccessShow value="Reboot">
                                        <el-dropdown-item v-if="scope.row.showReboot" @click="handleExit(scope.row.MachineId,scope.row.MachineName)"><el-icon><SwitchButton /></el-icon>{{$t('home.reboot')}}</el-dropdown-item>
                                    </AccessShow>
                                    <el-dropdown-item v-if="handleShowAccess(scope.row,accessList[scope.row.MachineId] || '0',values)" @click="handleAccess(scope.row)"><el-icon><Flag /></el-icon>{{$t('home.access')}}</el-dropdown-item>
                                    <AccessShow value="ApiPassword">
                                        <el-dropdown-item v-if="scope.row.isSelf" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon>{{$t('home.managerApi')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="ApiPasswordOther">
                                        <el-dropdown-item v-if="scope.row.isSelf==false" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon> {{$t('home.managerApi')}}</el-dropdown-item>
                                    </AccessShow>
                                    <el-dropdown-item @click="handleStopwatch(scope.row.MachineId,scope.row.MachineName)"><el-icon><Platform /></el-icon>{{$t('home.messenger',[scope.row.MachineName])}}</el-dropdown-item>
                                    <el-dropdown-item @click="handleStopwatch('',$t('home.server'))"><el-icon><Platform /></el-icon>{{$t('home.messengerServer')}}</el-dropdown-item>
                                    <el-dropdown-item @click="handleRoutes(scope.row.MachineId,scope.row.MachineName)"><el-icon><Paperclip /></el-icon>{{$t('home.tuntapRoute')}}</el-dropdown-item>
                                    <AccessShow value="FirewallSelf">
                                        <el-dropdown-item v-if="scope.row.isSelf" @click="handleFirewall(scope.row.MachineId,scope.row.MachineName)"><el-icon><CircleCheck /></el-icon>{{$t('home.firewall')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="FirewallOther">
                                        <el-dropdown-item v-if="scope.row.isSelf==false" @click="handleFirewall(scope.row.MachineId,scope.row.MachineName)"><el-icon><CircleCheck /></el-icon>{{$t('home.firewall')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="WakeupSelf">
                                        <el-dropdown-item v-if="scope.row.isSelf" @click="handleWakeup(scope.row.MachineId,scope.row.MachineName)"><el-icon><VideoPlay /></el-icon>{{$t('home.wakeup')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="WakeupOther">
                                        <el-dropdown-item v-if="scope.row.isSelf==false" @click="handleWakeup(scope.row.MachineId,scope.row.MachineName)"><el-icon><VideoPlay /></el-icon>{{$t('home.wakeup')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="Transport">
                                        <el-dropdown-item @click="handleTransport(scope.row.MachineId,scope.row.MachineName)"><el-icon><Orange /></el-icon>{{$t('home.protocol')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="ActionSelf">
                                        <el-dropdown-item v-if="scope.row.isSelf" @click="handleAction(scope.row.MachineId,scope.row.MachineName)"><el-icon><Lock /></el-icon>{{$t('home.action')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="ActionOther">
                                        <el-dropdown-item v-if="scope.row.isSelf==false" @click="handleAction(scope.row.MachineId,scope.row.MachineName)"><el-icon><Lock /></el-icon>{{$t('home.action')}}</el-dropdown-item>
                                    </AccessShow>
                                    <AccessShow value="Flow">
                                        <el-dropdown-item @click="handleFlow(scope.row.MachineId,scope.row.MachineName)"><el-icon><Histogram /></el-icon>{{$t('home.flowStatis')}}</el-dropdown-item>
                                    </AccessShow>
                                </template>
                                <AccessShow value="Remove">
                                    <el-dropdown-item v-if="scope.row.showDel" @click="handleDel(scope.row.MachineId,scope.row.MachineName)"><el-icon><Delete /></el-icon> {{$t('home.delete')}}</el-dropdown-item>
                                </AccessShow>
                            </el-dropdown-menu>
                        </template>
                    </el-dropdown>
                    
                </template>
            </el-table-column>
        </template>
    </AccessBoolean>
</template>

<script>
import { signInDel } from '@/apis/signin';
import { exit } from '@/apis/updater';
import { injectGlobalData } from '@/provide';
import { Delete,SwitchButton,ArrowDown, Flag,HelpFilled,Platform,Paperclip,CircleCheck,VideoPlay,Orange,Lock,Histogram } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed } from 'vue';
import { useAccess } from '../accesss/access';
import { setApiPassword } from '@/apis/access';
import { useFlow } from '../flow/flow';
import { useOper } from './oper';
import { useDevice } from '../device/devices';
import { useI18n } from 'vue-i18n';

export default {
    emits:['refresh','access'],
    components:{Delete,SwitchButton,ArrowDown,Flag,HelpFilled,Platform,Paperclip,CircleCheck,VideoPlay,Orange,Lock,Histogram},
    setup (props,{emit}) {
        
        const globalData = injectGlobalData();
        const {t} = useI18n();

        const devices = useDevice();
        const allAccess = useAccess();
        const myAccess = computed(()=>globalData.value.config.Client.AccessBits);
        const accessList = computed(()=>allAccess.value.list);
        
        const flow = useFlow();
        const oper  = useOper();
        
        const handleDel = (machineId,machineName)=>{
            ElMessageBox.confirm(t('home.deleteSure',[machineName]), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                signInDel(machineId).then(()=>{
                    emit('refresh');
                });
            }).catch(() => {});
        }
        const handleExit = (machineId,machineName)=>{
            ElMessageBox.confirm(t('home.closeSure',[machineName]), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                exit(machineId).then(()=>{
                    emit('refresh');
                })
            }).catch(() => {});
        }

        const handleShowAccess = (row,rowAccess,access)=>{ 
            let maxLength = Math.max(myAccess.value.length,rowAccess.length);
            let myValue = myAccess.value.padEnd(maxLength,'0').split('');
            let rowValue = rowAccess.padEnd(maxLength,'0').split('');
            return row.showAccess  && access.Access
            && myValue.map((v,i)=>{
                return (rowValue[i] == '1' && v == '1') || rowValue[i] == '0';
            }).filter(c=>c).length > 0;
        }
        const handleAccess = (row)=>{
            devices.deviceInfo = row;
            devices.showAccessEdit = true;
        }

        const handleApiPassword = (row)=>{
            ElMessageBox.prompt(t('home.newPassword'), t('home.setPassword',[row.MachineName]), {
                confirmButtonText:  t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                inputPattern:/^[0-9a-zA-Z]{1,32}$/,
                inputErrorMessage: '数字字母1-32位',
            }).then(({ value }) => {
                setApiPassword({machineId:row.MachineId,password:value}).then(()=>{
                    ElMessage.success(t('common.oper'));
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
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
        const handleTransport = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showTransport = true;
        }
        const handleAction = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showAction = true;
        }

        const handleFlow = (id,name)=>{
            oper.value.device.id = id;
            oper.value.device.name = name;
            oper.value.showFlow = true;
        }

        return {accessList,handleDel,handleExit,handleShowAccess,handleAccess,
           handleApiPassword,handleStopwatch,handleRoutes,
            handleFirewall,handleWakeup,
            handleTransport,handleAction,handleFlow
        }
    }
}
</script>

<style lang="stylus" scoped>

html.dark .dropdown{border-color:#575c61;}
.dropdown{
    border:1px solid #ddd;
    padding:.4rem;
    font-size:1.2rem;
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