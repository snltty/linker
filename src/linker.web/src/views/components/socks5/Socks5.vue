<template>
    <el-table-column prop="socks5" :label="$t('home.proxy')" width="160">
        <template #default="scope">
            <div v-if="scope.row.hook_socks5">
                <Socks5Show :config="true" :item="scope.row"></Socks5Show>
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