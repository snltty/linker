<template>

    <el-table-column prop="forward" label="端口转发">
        <template #default="scope">
            <template v-if="!scope.row.isSelf">
                <div v-if="hasForwardShowOther">
                    <ul class="list forward">
                        <template v-if="forward.list[scope.row.MachineId] && forward.list[scope.row.MachineId].length > 0">
                            <template v-for="(item, index) in forward.list[scope.row.MachineId]" :key="index">
                                <li>
                                    <a href="javascript:;" @click="handleEdit(scope.row.MachineId)" :class="{ green: item.Started }">
                                        <span>
                                            <span :class="{red:!!item.Msg}">{{item.Port}}</span>
                                            ->
                                            <span :class="{red:!!item.TargetMsg}">{{ item.TargetEP }}</span>
                                        </span>
                                    </a>
                                    <span> ({{ 1<<item.BufferSize }}KB)</span>
                                </li>
                            </template>
                        </template>
                        <template v-else>
                            <li><a href="javascript:;" title="管理你的端口转发" @click="handleEdit(scope.row.MachineId)">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
            </template>
            <template v-else>
                <div v-if="hasForwardShowSelf">
                    <ul class="list sforward">
                        <template v-if="sforward.list && sforward.list.length > 0">
                            <template v-for="(item, index) in sforward.list.slice(0,5)" :key="index">
                                <li :class="{red:!!item.Msg}">
                                    <a href="javascript:;" @click="handleSEdit()" :class="{ green: item.Started }">
                                        <span>
                                            <span :class="{red:!!item.Msg}">{{item.Domain || item.RemotePort}}</span>
                                            ->
                                            <span :class="{red:!!item.LocalMsg}">{{ item.LocalEP }}</span>
                                        </span>
                                    </a>
                                    <span> ({{ 1<<item.BufferSize }}KB)</span>
                                </li>
                            </template>
                        </template>
                        <template v-else>
                            <li><a href="javascript:;" title="管理你的服务器穿透" @click="handleSEdit()">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
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

        const handleEdit = (_machineId)=>{
            if(machineId.value === _machineId){
                if(!hasForwardSelf.value){
                    return;
                }
            }else{
                if(!hasForwardOther.value){
                    return;
                }
            }
            emit('edit',_machineId)
        }
        const handleSEdit = ()=>{
            if(!hasForwardSelf.value){
                return;
            }
            emit('sedit')
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
}
</style>