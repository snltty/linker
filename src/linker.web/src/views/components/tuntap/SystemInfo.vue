<template>
    <template v-if="item.hook_tuntap && item.hook_tuntap.systems">
        <div class="flex-1 relative skeleton-animation" :style="`animation-delay:${item.animationDelay}ms`">
            <span :title="item.hook_tuntap.SystemInfo" v-for="system in item.hook_tuntap.systems">
                <img :src="`./${system}.svg`" />
            </span>
        </div>
    </template>
    <template v-else-if="!item.hook_tuntap_load">
        <div class="flex-1 relative skeleton-animation">
            <el-skeleton animated >
                <template #template>
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                </template>
            </el-skeleton>
        </div>
    </template>
    <template v-else><div class="flex-1"></div></template>
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