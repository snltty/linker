<template>

    <el-table-column prop="forward" label="端口转发" width="190">
        <template #default="scope">
            <template v-if="scope.row.showForward">
                <div>
                    <ul class="list">
                        <template v-if="data[scope.row.MachineName] && data[scope.row.MachineName].length > 0">
                            <template v-for="(item, index) in data[scope.row.MachineName]" :key="index">
                                <li>
                                    <a href="javascript:;" @click="handleEdit(scope.row.MachineName)" :class="{ green: item.Started }">
                                        <template v-if="item.Started"><strong>{{ item.Port }}->{{ item.TargetEP
                                                }}</strong></template>
                                        <template v-else>{{ item.Port }}->{{ item.TargetEP }}</template>
                                    </a>
                                </li>
                            </template>
                        </template>
                        <template v-else>
                            <li><a href="javascript:;" @click="handleEdit(scope.row.MachineName)">暂无配置</a></li>
                        </template>
                    </ul>
                </div>
            </template>
            <template v-else>--</template>
        </template>
    </el-table-column>
</template>
<script>
export default {
    props: ['data'],
    emits: ['edit'],
    setup(props, { emit }) {

        const handleEdit = (machineName)=>{
            emit('edit',machineName)
        }

        return {
            data: props.data, handleEdit
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