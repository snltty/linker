<template>

    <el-table-column prop="forward" label="端口转发">
        <template #default="scope">
            <template v-if="scope.row.showForward">
                <div>
                    <ul class="list forward">
                        <template v-if="forward.list[scope.row.MachineId] && forward.list[scope.row.MachineId].length > 0">
                            <template v-for="(item, index) in forward.list[scope.row.MachineId]" :key="index">
                                <li :class="{error:!!item.Msg}">
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
            <template v-else-if="scope.row.showSForward">
                <div>
                    <ul class="list sforward">
                        <template v-if="sforward.list && sforward.list.length > 0">
                            <template v-for="(item, index) in sforward.list" :key="index">
                                <li :class="{error:!!item.Msg}">
                                    <a href="javascript:;" @click="handleSEdit()" :class="{ green: item.Started }">
                                        <template v-if="item.Started"><strong>{{ item.Domain || item.RemotePort }}->{{ item.LocalEP
                                                }}</strong></template>
                                        <template v-else>{{item.Domain || item.RemotePort }}->{{ item.LocalEP }}</template>
                                    </a>
                                </li>
                            </template>
                        </template>
                        <template v-else>
                            <li><a href="javascript:;" @click="handleSEdit()">暂无配置</a></li>
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
    emits: ['edit','sedit'],
    setup(props, { emit }) {

        const forward = inject('forward')
        const sforward = inject('sforward')
        const handleEdit = (machineId)=>{
            emit('edit',machineId)
        }
        const handleSEdit = ()=>{
            emit('sedit')
        }

        return {
            forward,sforward, handleEdit,handleSEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    text-decoration: underline;
    font-weight:bold;
}
a.green{color:green}

li.error{
    a{color:red;}
}
</style>