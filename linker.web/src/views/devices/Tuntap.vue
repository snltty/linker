<template>
    <el-table-column prop="tuntap" label="虚拟网卡" width="150">
        <template #default="scope">
            <div v-if="tuntap.list[scope.row.MachineId]">
                <div class="flex">
                    <div class="flex-1">
                        <a href="javascript:;" class="a-line" @click="handleTuntapIP(tuntap.list[scope.row.MachineId])">
                            <template v-if="tuntap.list[scope.row.MachineId].running">
                                <template v-if="tuntap.list[scope.row.MachineId].Error">
                                    <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="tuntap.list[scope.row.MachineId].Error">
                                        <template #reference>
                                            <strong class="red">{{ tuntap.list[scope.row.MachineId].IP }}</strong>
                                        </template>
                                    </el-popover>
                                </template>
                                <template v-else>
                                    <strong class="green">{{ tuntap.list[scope.row.MachineId].IP }}</strong>
                                </template>
                            </template>
                            <template v-else>
                                <span>{{ tuntap.list[scope.row.MachineId].IP }}</span>
                            </template>
                        </a>
                    </div>
                    <el-switch v-model="tuntap.list[scope.row.MachineId].running" :loading="tuntap.list[scope.row.MachineId].loading" disabled @click="handleTuntap(tuntap.list[scope.row.MachineId])"  size="small" inline-prompt active-text="O" inactive-text="F" > 
                    </el-switch>
                </div>
                <div>
                    <div v-for="(item,index) in  tuntap.list[scope.row.MachineId].LanIPs" :key="index">
                        {{ item }} / {{ tuntap.list[scope.row.MachineId].Masks[index] }}
                    </div>
                </div>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { stopTuntap, runTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { useTuntap } from './tuntap';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const tuntap = useTuntap();
        const handleTuntap = (tuntap) => {
            const fn = tuntap.running ? stopTuntap (tuntap.MachineId) : runTuntap(tuntap.MachineId);
            fn.then(() => {
                ElMessage.success('操作成功！');
            }).catch(() => {
                ElMessage.error('操作失败！');
            })
        }
        const handleTuntapIP = (tuntap) => {
            emit('edit',tuntap);
        }
        const handleTuntapRefresh = ()=>{
            emit('refresh');
        }
       
        return {
            tuntap,  handleTuntap, handleTuntapIP,handleTuntapRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.el-input{
    width:8rem;
}
</style>