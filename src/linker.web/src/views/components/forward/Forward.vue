<template>
    <el-table-column prop="forward" :label="$t('forward.port')" width="96">
        <template #default="scope">
            <template v-if="scope.row &&scope.row.hook_counter">
                <AccessBoolean value="ForwardOther,ForwardSelf">
                    <template #default="{values}">
                        <div class="skeleton-animation" :style="`animation-delay:${scope.row.animationDelay}ms`" v-if="values.ForwardOther || (values.ForwardSelf && scope.row.isSelf)">
                            <div class="nowrap">
                                <ConnectionShow :row="scope.row" transactionId="forward"></ConnectionShow>
                                <a href="javascript:;" :class="{green:scope.row.hook_counter.forward>0}" @click="handleEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span >{{$t('forward.port')}}({{scope.row.hook_counter.forward>99 ? '99+' : scope.row.hook_counter.forward}})</span>
                                </a>
                            </div>
                            <div class="nowrap">
                                <a href="javascript:;" :class="{green:scope.row.hook_counter.reverse>0}" @click="handleSEdit(scope.row.MachineId,scope.row.MachineName,values)">
                                    <span >{{$t('forward.server')}}({{scope.row.hook_counter.reverse>99 ? '99+' :scope.row.hook_counter.reverse}})</span>
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
                            <p class="nowrap"><el-skeleton-item variant="text" class="w-50-" /></p>
                            <p class="nowrap"><el-skeleton-item variant="text" class="w-50-" /></p>
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
import { useReverse } from './reverse';
import { computed } from 'vue';
import ConnectionShow from '../tunnel/ConnectionShow.vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

export default {
    components:{ConnectionShow},
    setup() {

        const {t} = useI18n();
        const forward = useForward()
        const reverse = useReverse()
        
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);

        const handleEdit = (_machineId,_machineName,access)=>{
            if(machineId.value === _machineId){
                if(!access.ForwardSelf){
                    ElMessage.success(t('common.access'));  
                    return;
                }
            }else{
                if(!access.ForwardOther){
                    ElMessage.success(t('common.access'));
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
                ElMessage.success(t('common.access'));
                return;
            }
            }else{
                if(!access.ForwardOther){
                ElMessage.success(t('common.access'));
                return;
            }
            }
            reverse.value.machineid = _machineId;
            reverse.value.machineName = _machineName;
            reverse.value.showEdit = true;
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
    
}
.nowrap{line-height:1.8rem;}
</style>