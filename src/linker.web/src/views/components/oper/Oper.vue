<template>
    <AccessBoolean value="Access">
        <template #default="{values}">
            <el-table-column :label="$t('home.oper')"  fixed="right" min-width="110">
                <template #default="scope">
                    <div v-if="scope.row">
                        <div class="numbers">
                            <el-row>
                                <template v-if="scope.row.Connected">
                                    <FirewallOper :item="scope.row"></FirewallOper>
                                    <WakeupOper :item="scope.row"></WakeupOper>
                                    <TransportOper :item="scope.row"></TransportOper>
                                </template>
                                <el-col :span="12">
                                     <el-dropdown size="small" v-if="scope.row.MachineId">
                                        <div class="dropdown">
                                            <span>...</span>
                                            <el-icon class="el-icon--right">
                                                <ArrowDown />
                                            </el-icon>
                                        </div>
                                        <template #dropdown>
                                            <el-dropdown-menu>
                                                <template v-if="scope.row.Connected">
                                                    <AccessShow value="Reboot">
                                                        <el-dropdown-item @click="handleExit(scope.row)"><el-icon><SwitchButton /></el-icon>{{$t('home.reboot')}}</el-dropdown-item>
                                                    </AccessShow>
                                                    <el-dropdown-item v-if="handleShowAccess(scope.row,values)" @click="handleAccess(scope.row)"><el-icon><Flag /></el-icon>{{$t('home.access')}}</el-dropdown-item>
                                                    <AccessShow value="ApiPassword">
                                                        <el-dropdown-item v-if="scope.row.isSelf" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon>{{$t('home.managerApi')}}</el-dropdown-item>
                                                    </AccessShow>
                                                    <AccessShow value="ApiPasswordOther">
                                                        <el-dropdown-item v-if="scope.row.isSelf==false" @click="handleApiPassword(scope.row)"><el-icon><HelpFilled /></el-icon> {{$t('home.managerApi')}}</el-dropdown-item>
                                                    </AccessShow>
                                                    <ActionOper :item="scope.row"></ActionOper>
                                                    <FlowOper :item="scope.row"></FlowOper>
                                                </template>
                                                <AccessShow value="Remove">
                                                    <el-dropdown-item v-if="scope.row.showDel" @click="handleDel(scope.row)"><el-icon><Delete /></el-icon> {{$t('home.delete')}}</el-dropdown-item>
                                                </AccessShow>
                                            </el-dropdown-menu>
                                        </template>
                                    </el-dropdown>
                                </el-col>
                            </el-row>
                        </div>
                    </div>
                    <div class="device-remark"></div>
                </template>
            </el-table-column>
        </template>
    </AccessBoolean>
</template>

<script>
import { signInDel } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { Delete,SwitchButton,ArrowDown, Flag,HelpFilled,Platform,Paperclip,CircleCheck,VideoPlay,Orange,Lock,Histogram } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { setApiPassword } from '@/apis/access';
import { useDevice } from '../device/devices';
import { useI18n } from 'vue-i18n';
import { exit } from '@/apis/updater';
import FirewallOper from '../firewall/Oper.vue'
import WakeupOper from '../wakeup/Oper.vue'
import TransportOper from '../transport/Oper.vue'
import ActionOper from '../action/Oper.vue'
import FlowOper from '../flow/Oper.vue'

export default {
    emits:['refresh','access'],
    components:{Delete,SwitchButton,ArrowDown
        ,Flag,HelpFilled,Platform
        ,Paperclip,CircleCheck
        ,VideoPlay,Orange,Lock,Histogram
        ,FirewallOper,WakeupOper,TransportOper,ActionOper,FlowOper},
    setup (props,{emit}) {
        
        const globalData = injectGlobalData();
        const {t} = useI18n();

        const devices = useDevice();
        
        const handleDel = (row)=>{
            ElMessageBox.confirm(t('home.deleteSure',[row.MachineName]), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                signInDel(row.MachineId).then(()=>{
                    emit('refresh');
                });
            }).catch(() => {});
        }
        
         const handleExit = (row)=>{
            ElMessageBox.confirm(t('home.closeSure',[row.MachineName]), t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                exit(row.MachineId);
            }).catch(() => {});
        }


        const handleShowAccess = (row,access)=>{ 
            const rowAccess = row.hook_accesss || '';
            const myAccess = globalData.value.config.Client.AccessBits;

            let maxLength = Math.max(myAccess.length,rowAccess.length);
            let myValue = myAccess.padEnd(maxLength,'0').split('').map(c=>+c);
            let rowValue = rowAccess.padEnd(maxLength,'0').split('').map(c=>+c);
            return row.showAccess  && access.Access
            && myValue.map((v,i)=>{
                return (rowValue[i] + v >=1 && v == 1);
            }).filter(c=>!c).length == 0;
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



        return {handleDel,handleExit,handleShowAccess,handleAccess,
           handleApiPassword
        }
    }
}
</script>

<style lang="stylus" scoped>

html.dark .dropdown{border-color:#575c61;}
.dropdown{
    padding:.4rem;
    font-size:1.2rem;
    position:relative;
    .el-icon{
        vertical-align:middle;
    }
}
</style>