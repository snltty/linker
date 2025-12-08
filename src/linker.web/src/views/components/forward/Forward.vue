<template>
    <el-table-column prop="forward" :label="$t('home.forward')" width="80">
        <template #default="scope">
            <template v-if="scope.row &&scope.row.hook_counter">
                <AccessBoolean value="ForwardOther,ForwardSelf">
                    <template #default="{values}">
                        <div class="skeleton-animation" :style="`animation-delay:${scope.row.animationDelay}ms`" v-if="values.ForwardOther || (values.ForwardSelf && scope.row.isSelf)">
                            <div class="nowrap">
                                <ConnectionShow :row="scope.row" transactionId="forward"></ConnectionShow>
                                <a href="javascript:;" :class="{green:scope.row.hook_counter.forward>0}" @click="handleEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span :class="{gateway:scope.row.hook_counter.forward>0}">{{$t('home.forwardPort')}}({{scope.row.hook_counter.forward>99 ? '99+' : scope.row.hook_counter.forward}})</span>
                                </a>
                            </div>
                            <div class="nowrap">
                                <a href="javascript:;" :class="{green:scope.row.hook_counter.sforward>0}" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span :class="{gateway:scope.row.hook_counter.sforward>0 }">{{$t('home.forwardServer')}}({{scope.row.hook_counter.sforward>99 ? '99+' :scope.row.hook_counter.sforward}})</span>
                                </a>
                            </div>
                        </div>
                    </template>
                </AccessBoolean>
            </template>
            <template v-else-if="scope.row &&!scope.row.hook_counter_load">
                <div class="skeleton-animation">
                    <el-skeleton animated >
                        <template #template>
                            <p><el-skeleton-item variant="text" style="width: 50%;" /></p>
                            <p><el-skeleton-item variant="text" style="width: 50%" /></p>
                        </template>
                    </el-skeleton>
                </div>
            </template>
            <div class="device-remark"></div>
        </template>
    </el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
import { useForward } from './forward';
import { useSforward } from './sforward';
import { computed } from 'vue';
import ConnectionShow from '../tunnel/ConnectionShow.vue';
import { ElMessage } from 'element-plus';

export default {
    components:{ConnectionShow},
    setup() {

        const forward = useForward()
        const sforward = useSforward()
        
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);

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
        return {
           handleEdit,handleSEdit
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