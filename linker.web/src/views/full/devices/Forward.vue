<template>
    <el-table-column prop="forward" label="转发/穿透">
        <template #default="scope">
            <template v-if="scope.row.Connected">
                <template v-if="scope.row.isSelf && (hasForwardShowSelf || hasForwardSelf)">
                    <div>
                        <a href="javascript:;" title="管理自己的端口转发" @click="handleEdit(scope.row.MachineId,scope.row.MachineName)">
                            端口转发({{forward.list[scope.row.MachineId]>99 ? '99+' : forward.list[scope.row.MachineId]}})
                        </a>
                    </div>
                    <div>
                        <a href="javascript:;" title="管理自己的内网穿透" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName)">
                            内网穿透({{sforward.list[scope.row.MachineId]>99 ? '99+' : sforward.list[scope.row.MachineId]}})
                        </a>
                    </div>
                </template>
                <template v-else-if="hasForwardShowOther || hasForwardOther">
                    <div>
                        <a href="javascript:;" title="管理自己的端口转发" @click="handleEdit(scope.row.MachineId,scope.row.MachineName)">
                            端口转发({{forward.list[scope.row.MachineId]>99 ? '99+' : forward.list[scope.row.MachineId]}})
                        </a>
                    </div>
                    <div>
                        <a href="javascript:;" title="管理自己的内网穿透" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName)">
                            内网穿透({{sforward.list[scope.row.MachineId]>99 ? '99+' : sforward.list[scope.row.MachineId]}})
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
            forward,sforward,hasForwardShowSelf,hasForwardShowOther, handleEdit,handleSEdit,handleForwardRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    text-decoration: underline;
    font-weight:bold;
    &+a{margin-left:1rem}
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