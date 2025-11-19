<template>
    <div>
        <template v-if="item.hook_tuntap && item.hook_tuntap.systems">
            <template v-for="system in item.hook_tuntap.systems">
                <span :title="item.hook_tuntap.SystemInfo">
                    <img :src="`./${system}.svg`" />
                </span>
            </template>
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
</style>