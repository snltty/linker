<template>
    <el-table-column label="操作" width="74" fixed="right">
        <template #default="scope">
            <el-dropdown size="small">
                <div class="dropdown">
                    <!-- <span class="badge">1</span> -->
                    <span>操作</span>
                    <el-icon class="el-icon--right">
                        <ArrowDown />
                    </el-icon>
                </div>
                <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item v-if="scope.row.showReboot" @click="handleExit(scope.row.MachineId,scope.row.MachineName)"><el-icon><SwitchButton /></el-icon> 重启</el-dropdown-item>
                    <el-dropdown-item v-if="scope.row.showDel" @click="handleDel(scope.row.MachineId,scope.row.MachineName)"><el-icon><Delete /></el-icon> 删除</el-dropdown-item>
                </el-dropdown-menu>
                </template>
            </el-dropdown>
            
        </template>
    </el-table-column>
</template>

<script>
import { signInDel } from '@/apis/signin';
import { exit } from '@/apis/updater';
import { Delete,SwitchButton,ArrowDown } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus';

export default {
    emits:['refresh'],
    components:{Delete,SwitchButton,ArrowDown},
    setup (props,{emit}) {
        
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

        return {handleDel,handleExit}
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