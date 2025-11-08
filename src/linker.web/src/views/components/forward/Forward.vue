<template>
    <el-table-column prop="forward" :label="forward.show?$t('home.forward'):''" >
        <template #default="scope">
            <template v-if="forward.show && scope.row.Connected">
                <AccessBoolean value="ForwardOther,ForwardSelf">
                    <template #default="{values}">
                        <template v-if="values.ForwardOther || (values.ForwardSelf && scope.row.isSelf)">
                            <div class="nowrap">
                                <ConnectionShow :data="connections.list[scope.row.MachineId]" :row="scope.row" transitionId="forward"></ConnectionShow>
                                <a href="javascript:;" :class="{green:forward.list[scope.row.MachineId]>0}" @click="handleEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span :class="{gateway:forward.list[scope.row.MachineId]>0}">{{$t('home.forwardPort')}}({{forward.list[scope.row.MachineId]>99 ? '99+' : forward.list[scope.row.MachineId]}})</span>
                                </a>
                            </div>
                            <div class="nowrap">
                                <a href="javascript:;" :class="{green:sforward.list[scope.row.MachineId]>0}" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span :class="{gateway:sforward.list[scope.row.MachineId]>0 }">{{$t('home.forwardServer')}}({{sforward.list[scope.row.MachineId]>99 ? '99+' : sforward.list[scope.row.MachineId]}})</span>
                                </a>
                            </div>
                        </template>
                    </template>
                </AccessBoolean>
            </template>
        </template>
    </el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { useForward } from './forward';
import { useSforward } from './sforward';
import { computed } from 'vue';
import { useForwardConnections } from '../connection/connections';
import ConnectionShow from '../connection/ConnectionShow.vue';
import { ElMessage } from 'element-plus';

export default {
    emits: ['edit','sedit'],
    components:{ConnectionShow},
    setup(props, { emit }) {

        const forward = useForward()
        const sforward = useSforward()
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const connections = useForwardConnections();

        const handleEdit = (_machineId,_machineName,access)=>{
            if(machineId.value === _machineId){
                if(!access.ForwardSelf){
                    ElMessage.success('无权限');
                    return;
                }
            }else{
                if(!access.ForwardOther){
                    ElMessage.success('无权限');
                    return;
                }
            }
            forward.value.machineId = _machineId;
            forward.value.machineName = _machineName;
            forward.value.showEdit = true;
        }
        const handleSEdit = (_machineId,_machineName,access)=>{
            if(machineId.value === _machineId){
                if(!access.ForwardSelf){
                ElMessage.success('无权限');
                return;
            }
            }else{
                if(!access.ForwardOther){
                ElMessage.success('无权限');
                return;
            }
            }
            sforward.value.machineid = _machineId;
            sforward.value.machineName = _machineName;
            sforward.value.showEdit = true;
        }
        const handleForwardRefresh = ()=>{
            emit('refresh');
        }

        return {
            forward,sforward,connections, handleEdit,handleSEdit,handleForwardRefresh
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