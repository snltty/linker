<template>
    <el-table-column prop="socks5" :label="$t('home.proxy')" width="160">
        <template #default="scope">
            <template v-if="scope.row &&scope.row.hook_socks5">
                <div class="skeleton-animation" :style="`animation-delay:${scope.row.animationDelay}ms`">
                    <Socks5Show :config="true" :item="scope.row"></Socks5Show>
                </div>
            </template>
            <template v-else-if="scope.row &&!scope.row.hook_socks5_load">
                <div class="skeleton-animation">
                    <el-skeleton animated >
                        <template #template>
                            <el-skeleton-item variant="text" style="width: 60%;" />
                            <el-skeleton-item variant="text" style="width: 20%;margin-left:20%" />
                        </template>
                    </el-skeleton>
                </div>
            </template>
            <div class="device-remark"></div>
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