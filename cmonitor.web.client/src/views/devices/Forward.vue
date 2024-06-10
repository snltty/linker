<template>

    <el-table-column prop="forward" label="端口转发" width="190">
        <template #default="scope">
            <template v-if="scope.row.showForward">
                <div>
                    <ul class="list">
                        <template v-if="forward.list[scope.row.MachineId] && forward.list[scope.row.MachineId].length > 0">
                            <template v-for="(item, index) in forward.list[scope.row.MachineId]" :key="index">
                                <li>
                                    <a href="javascript:;" @click="handleEdit(scope.row.MachineId)" :class="{ green: item.Started }">
                                        <template v-if="item.Started"><strong>{{ item.Port }}->{{ item.TargetEP
                                                }}</strong></template>
                                        <template v-else>{{ item.Port }}->{{ item.TargetEP }}</template>
                                    </a>
                                </li>
                            </template>
                        </template>
                        <template v-else>
                            <li><a href="javascript:;" @click="handleEdit(scope.row.MachineId)">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
            </template>
            <template v-else>--</template>
        </template>
    </el-table-column>
</template>
<script>
import { inject } from 'vue';

export default {
    emits: ['edit'],
    setup(props, { emit }) {

        const forward = inject('forward')
        const handleEdit = (machineId)=>{
            emit('edit',machineId)
        }

        return {
            forward, handleEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    text-decoration: underline;
}
a.green{color:green}
</style>