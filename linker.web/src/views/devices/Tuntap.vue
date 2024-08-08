<template>
    <el-table-column prop="tuntap" label="虚拟网卡" width="150">
        <template #default="scope">
            <div v-if="tuntap.list[scope.row.MachineId]">
                <div class="flex">
                    <div class="flex-1">
                        <a href="javascript:;" class="a-line" @click="handleTuntapIP(tuntap.list[scope.row.MachineId])" :title="tuntap.list[scope.row.MachineId].Gateway?'我在路由器上，所以略有不同':''">
                            <template v-if="tuntap.list[scope.row.MachineId].Error">
                                <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="tuntap.list[scope.row.MachineId].Error">
                                    <template #reference>
                                        <strong class="red">{{ tuntap.list[scope.row.MachineId].IP }}</strong>
                                    </template>
                                </el-popover>
                            </template>
                            <template v-else>
                                <template v-if="tuntap.list[scope.row.MachineId].running">
                                    <strong class="green" :class="{gateway:tuntap.list[scope.row.MachineId].Gateway}">{{ tuntap.list[scope.row.MachineId].IP }}</strong>
                                </template>
                                <template v-else>
                                    <strong :class="{gateway:tuntap.list[scope.row.MachineId].Gateway}">{{ tuntap.list[scope.row.MachineId].IP }}</strong>
                                </template>
                            </template>
                        </a>
                    </div>
                    <template v-if="tuntap.list[scope.row.MachineId].loading">
                        <div>
                            <el-icon size="14" class="loading"><Loading /></el-icon>
                        </div>
                    </template>
                    <template v-else>
                        <el-switch v-model="tuntap.list[scope.row.MachineId].running" :loading="tuntap.list[scope.row.MachineId].loading" disabled @click="handleTuntap(tuntap.list[scope.row.MachineId])"  size="small" inline-prompt active-text="O" inactive-text="F" > 
                        </el-switch>
                    </template>
                </div>
                <div>
                    <template v-if="tuntap.list[scope.row.MachineId].Error1">
                        <el-popover placement="top" title="msg" width="20rem"  trigger="hover" :content="tuntap.list[scope.row.MachineId].Error1">
                            <template #reference>
                                <div class="yellow">
                                    <template v-for="(item,index) in  tuntap.list[scope.row.MachineId].LanIPs" :key="index">
                                        <div>
                                            {{ item }} / {{ tuntap.list[scope.row.MachineId].Masks[index] }}
                                        </div>
                                    </template>
                                </div>
                            </template>
                        </el-popover>
                    </template>
                    <template v-else>
                        <div>
                            <template v-for="(item,index) in  tuntap.list[scope.row.MachineId].LanIPs" :key="index">
                                <div>
                                    {{ item }} / {{ tuntap.list[scope.row.MachineId].Masks[index] }}
                                </div>
                            </template>
                        </div>
                    </template>
                </div>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { stopTuntap, runTuntap } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { useTuntap } from './tuntap';
import {Loading} from '@element-plus/icons-vue'
export default {
    emits: ['edit','refresh'],
    components:{Loading},
    setup(props, { emit }) {

        const tuntap = useTuntap();
        const handleTuntap = (tuntap) => {
            const fn = tuntap.running ? stopTuntap (tuntap.MachineId) : runTuntap(tuntap.MachineId);
            tuntap.loading = true;
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

@keyframes loading {
    from{transform:rotate(0deg)}
    to{transform:rotate(360deg)}
}
.el-icon.loading{
    vertical-align:middle;font-weight:bold;
    animation:loading 1s linear infinite;
}

.el-switch.is-disabled{opacity :1;}
.el-input{
    width:8rem;
}

.gateway{
    background:linear-gradient(90deg, #c5b260, #858585, #c5b260, #858585);
    -webkit-background-clip:text;
    -webkit-text-fill-color:hsla(0,0%,100%,0);
    &.green{
        background:linear-gradient(90deg, #e4bb10, #008000, #e4bb10, #008000);
        -webkit-background-clip:text;
        -webkit-text-fill-color:hsla(0,0%,100%,0);
    }
}

</style>