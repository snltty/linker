<template>
    <el-col :span="6" class="time">
        <span>fps:{{data.Report.fps}}、{{data.Report.ping}}ms</span>
    </el-col>
</template>

<script>
export default {
    pluginName:'cmonitor.plugin.report.',
    sort: -1,
    props: ['data'],
    setup(props) {
        return {
            data: props.data
        }
    }
}
</script>

<style lang="stylus" scoped>
.time {
    text-align: left !important;
    color: #666;
    line-height: 2rem;

    &>span {
        font-size: 1.2rem;
    }
}
</style>