<template>

    <el-table-column prop="forward" label="端口转发">
        <template #default="scope">
            <template v-if="!scope.row.isSelf">
                <div>
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
                            <li><a href="javascript:;" @click="handleEdit(scope.row.MachineId)">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
            </template>
            <template v-else>
                <div>
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
                            <li><a href="javascript:;" @click="handleSEdit()">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
            </template>
        </template>
    </el-table-column>
</template>
<script>
import { useForward } from './forward';
import { useSforward } from './sforward';

export default {
    emits: ['edit','sedit'],
    setup(props, { emit }) {

        const forward = useForward()
        const sforward = useSforward()
        const handleEdit = (machineId)=>{
            emit('edit',machineId)
        }
        const handleSEdit = ()=>{
            emit('sedit')
        }
        const handleForwardRefresh = ()=>{
            emit('refresh');
        }

        return {
            forward,sforward, handleEdit,handleSEdit,handleForwardRefresh
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