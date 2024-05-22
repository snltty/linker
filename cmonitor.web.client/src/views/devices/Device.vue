<template>
<el-table-column prop="MachineName" label="设备">
    <template #header>
        <div class="flex">
            <span class="flex-1">设备</span>
            <el-button size="small" @click="handleRefresh"><el-icon><Refresh /></el-icon></el-button>
        </div>
    </template>
    <template #default="scope">
        <div>
            <p>
                <a href="javascript:;" @click="handleEdit(scope.row)" :class="{green:scope.row.Connected}">{{scope.row.MachineName }}</a>
                <strong v-if="scope.row.isSelf"> -- (本机)</strong>
            </p>
            <p>{{ scope.row.IP }}</p>
        </div>
    </template>
</el-table-column>
</template>
<script>
export default {
    emits:['edit','refresh'],
    setup(props,{emit}) {

        const handleEdit = (row)=>{
            emit('edit',row)
        }
        const handleRefresh = ()=>{
            emit('refresh')
        }
        return {
            handleEdit,handleRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
a{
    color:#666;
    text-decoration: underline;
    font-weight:bold;
}
a.green{color:green}
</style>