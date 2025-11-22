<template>
    <el-table-column prop="socks5" :label="$t('home.proxy')" width="160">
        <template #default="scope">
            <div>
                <template v-if="scope.row.hook_socks5">
                    <Socks5Show :config="true" :item="scope.row"></Socks5Show>
                </template>
                <template v-else>
                    <el-skeleton animated >
                        <template #template>
                            <el-skeleton-item variant="text" style="width: 60%;" />
                            <el-skeleton-item variant="text" style="width: 20%;margin-left:20%" />
                        </template>
                    </el-skeleton>
                </template>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import Socks5Show from './Socks5Show.vue';
export default {
    emits: ['edit','refresh'],
    components:{Socks5Show},
    setup() {

        return {}
    }
}
</script>
<style lang="stylus" scoped>
</style>