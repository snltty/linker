<template>
    <span class="speed">
        <span>
            <el-icon>
                <Upload />
            </el-icon>
            {{data.Hijack.UploadText}}
        </span>
        <span>
            <el-icon>
                <Download />
            </el-icon>
            {{data.Hijack.DownloadText}}
        </span>
    </span>
</template>

<script>
export default {
    props: ['data'],
    setup(props) {

        return {
            data: props.data
        }
    }
}
</script>

<style lang="stylus" scoped>
.speed {
    font-size: 1.3rem;
    padding-top: 0.1rem;
    color: #333;

    span:nth-child(1) {
        margin-right: 1rem;
    }

    .el-icon {
        vertical-align: middle;
        margin-top: -2px;
    }
}
</style>