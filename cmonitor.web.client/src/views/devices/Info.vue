<template>
    <el-table-column prop="LastSignIn" label="其它" width="140">
        <template #default="scope">
            <div>
                <p>{{ scope.row.LastSignIn }}</p>
                <p>v{{ scope.row.Version }}</p>
            </div>
        </template>
    </el-table-column>
</template>
<script>
export default {
    setup(props) {
        return {
            
        }
    }
}
</script>
<style lang="stylus" scoped>
    
</style>