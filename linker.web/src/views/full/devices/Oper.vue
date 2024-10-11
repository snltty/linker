<template>
    <el-table-column label="操作" width="74" fixed="right">
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
                    <el-dropdown-item v-if="handleShowAccess(scope.row,accessList[scope.row.MachineId] || 0)" @click="handleAccess(scope.row)"><el-icon><Flag /></el-icon> 权限</el-dropdown-item>
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
import { Delete,SwitchButton,ArrowDown, Flag } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus';
import { computed } from 'vue';
import { useAccess } from './access';

export default {
    emits:['refresh','access'],
    components:{Delete,SwitchButton,ArrowDown,Flag},
    setup (props,{emit}) {
        
        const globalData = injectGlobalData();

        const allAccess = useAccess();
        const myAccess = computed(()=>globalData.value.config.Client.Access);
        const hasAccess = computed(()=>globalData.value.hasAccess('Access')); 
        const accessList = computed(()=>allAccess.value.list);
        
        const hasReboot = computed(()=>globalData.value.hasAccess('Reboot')); 
        const hasRemove = computed(()=>globalData.value.hasAccess('Remove')); 
        
        

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


        const handleShowAccess = (row,rowAccess)=>{
            return row.showAccess 
            && hasAccess.value 
            && rowAccess >= 0 
            // 它的权限删掉我的权限==0，我至少拥有它的全部权限，它是我的子集，我有权管它
            && +(((~BigInt(myAccess.value)) & BigInt(rowAccess)).toString()) == 0;
        }
        const handleAccess = (row)=>{
            emit('access',row);
        }

        return {accessList,handleDel,handleExit,hasReboot,hasRemove,hasAccess,handleShowAccess,handleAccess}
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