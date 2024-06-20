<template>
    <i class="user-name" v-if="data.Share.UserName.Value"> - {{data.Share.UserName.Value}}</i>
</template>

<script>
export default {
    pluginName:'share',
    props: ['data'],
    setup(props) {

        return {
            data: props.data
        }
    }
}
</script>

<style lang="stylus" scoped>
i.user-name {
    color: #666;
}
</style>