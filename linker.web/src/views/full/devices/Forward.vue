<template>
    <el-table-column prop="forward" label="转发/穿透">
        <template #default="scope">
            <template v-if="scope.row.Connected">
                <template v-if="scope.row.isSelf && (hasForwardShowSelf || hasForwardSelf)">
                    <div>
                        <template v-if="connections.list[scope.row.MachineId] && connections.list[scope.row.MachineId].Connected">
                            <template v-if="connections.list[scope.row.MachineId].Type == 0">
                                <span class="point p2p" title="打洞直连"></span>
                            </template>
                            <template v-else-if="connections.list[scope.row.MachineId].Type == 1">
                                <span class="point relay" title="中继连接"></span>
                            </template>
                            <template v-else-if="connections.list[scope.row.MachineId].Type == 2">
                                <span class="point node" title="节点连接"></span>
                            </template>
                        </template>
                        <template v-else>
                            <span class="point" title="未连接"></span>
                        </template>
                        <a href="javascript:;" title="管理自己的端口转发" :class="{green:forward.list[scope.row.MachineId]>0 }" @click="handleEdit(scope.row.MachineId,scope.row.MachineName)">
                            <span :class="{gateway:forward.list[scope.row.MachineId]>0}">端口转发({{forward.list[scope.row.MachineId]>99 ? '99+' : forward.list[scope.row.MachineId]}})</span>
                        </a>
                    </div>
                    <div>
                        <a href="javascript:;" title="管理自己的内网穿透" :class="{green:sforward.list[scope.row.MachineId]>0}" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName)">
                            <span :class="{gateway:sforward.list[scope.row.MachineId]>0 }">内网穿透({{sforward.list[scope.row.MachineId]>99 ? '99+' : sforward.list[scope.row.MachineId]}})</span>
                        </a>
                    </div>
                </template>
                <template v-else-if="hasForwardShowOther || hasForwardOther">
                    <div>
                        <template v-if="connections.list[scope.row.MachineId] && connections.list[scope.row.MachineId].Connected">
                            <template v-if="connections.list[scope.row.MachineId].Type == 0">
                                <span class="point p2p" title="打洞直连"></span>
                            </template>
                            <template v-else-if="connections.list[scope.row.MachineId].Type == 1">
                                <span class="point relay" title="中继连接"></span>
                            </template>
                            <template v-else-if="connections.list[scope.row.MachineId].Type == 2">
                                <span class="point node" title="节点连接"></span>
                            </template>
                        </template>
                        <template v-else>
                            <span class="point" title="未连接"></span>
                        </template>
                        <a href="javascript:;" title="管理自己的端口转发" :class="{green:forward.list[scope.row.MachineId]>0}" @click="handleEdit(scope.row.MachineId,scope.row.MachineName)">
                            <span :class="{gateway:forward.list[scope.row.MachineId]>0}">端口转发({{forward.list[scope.row.MachineId]>99 ? '99+' : forward.list[scope.row.MachineId]}})</span>
                        </a>
                    </div>
                    <div>
                        <a href="javascript:;" title="管理自己的内网穿透" :class="{green:sforward.list[scope.row.MachineId]>0}" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName)">
                            <span :class="{gateway:sforward.list[scope.row.MachineId]>0 }">内网穿透({{sforward.list[scope.row.MachineId]>99 ? '99+' : sforward.list[scope.row.MachineId]}})</span>
                        </a>
                    </div>
                </template>
            </template>
        </template>
    </el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { useForward } from './forward';
import { useSforward } from './sforward';
import { computed } from 'vue';
import { useForwardConnections } from './connections';

export default {
    emits: ['edit','sedit'],
    setup(props, { emit }) {

        const forward = useForward()
        const sforward = useSforward()
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const hasForwardShowSelf = computed(()=>globalData.value.hasAccess('ForwardShowSelf')); 
        const hasForwardShowOther = computed(()=>globalData.value.hasAccess('ForwardShowOther')); 
        const hasForwardSelf = computed(()=>globalData.value.hasAccess('ForwardSelf')); 
        const hasForwardOther = computed(()=>globalData.value.hasAccess('ForwardOther')); 
        const connections = useForwardConnections();

        const handleEdit = (_machineId,_machineName)=>{
            if(machineId.value === _machineId){
                if(!hasForwardSelf.value){
                    return;
                }
            }else{
                if(!hasForwardOther.value){
                    return;
                }
            }
            emit('edit',[_machineId,_machineName])
        }
        const handleSEdit = (_machineId,_machineName)=>{
            if(machineId.value === _machineId){
                if(!hasForwardSelf.value){
                    return;
                }
            }else{
                if(!hasForwardOther.value){
                    return;
                }
            }
            emit('sedit',[_machineId,_machineName])
        }
        const handleForwardRefresh = ()=>{
            emit('refresh');
        }

        return {
            forward,sforward,hasForwardShowSelf,hasForwardShowOther,connections, handleEdit,handleSEdit,handleForwardRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    text-decoration: underline;
    &+a{margin-left:1rem}
    &.green{
        font-weight:bold;
    }
}
</style>