<template>
    <div class="flex-1">
        <template v-if="item.hook_tuntap && item.hook_tuntap.systems">
            <template v-for="system in item.hook_tuntap.systems">
                <span :title="item.hook_tuntap.SystemInfo">
                    <img :src="`./${system}.svg`" />
                </span>
            </template>
        </template>
        <template v-else>
            <el-skeleton animated >
                <template #template>
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                </template>
            </el-skeleton>
        </template>
    </div>
</template>

<script>
export default {
    props:['item'],
    setup () {
        return {}
    }
}
</script>

<style lang="stylus" scoped>
img{
    height:1.4rem;
    vertical-align: middle;
    margin-right:.1rem
}
.el-skeleton-item{
    vertical-align: middle;width: 20%;margin-right: 2%;
}
</style>