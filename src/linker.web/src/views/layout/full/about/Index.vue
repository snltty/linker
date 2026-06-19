<template>
    <div class="about-wrap h-100">
        <el-card shadow="never" class="h-100 flex flex-column flex-nowrap">
            <template #header>
                <div class="flex">
                    <span class="flex-1">{{$t('head.about')}}</span>
                </div>
            </template>
            <div class="absolute scrollbar">
                <div class="inner">
                    <el-descriptions size="small" title="" :column="1" border label-width="0">
                        <el-descriptions-item label="" class-name="hide"><a href="javascript:;">©linker by snltty</a></el-descriptions-item>
                        <el-descriptions-item label="">
                            <a href="https://github.com/snltty/linker" class="a-line" target="_blank">Github</a>
                            <a href="https://linker.snltty.com" class="a-line" target="_blank">{{$t('status.website')}}</a>
                            <a href="https://linker-doc.snltty.com" class="a-line" target="_blank">{{$t('status.doc')}}</a>
                        </el-descriptions-item>
                        <el-descriptions-item label="" class="">
                            <a href="https://ifdian.net/a/snltty" class="memory a-line flex flex-items-center" :title="$t('status.support')" target="_blank">
                                <img src="@/assets/dianchi.svg" alt="memory" width="30" />
                                <span>{{$t('status.support')}}</span>
                            </a>
                        </el-descriptions-item>
                        <el-descriptions-item label="">
                            <img src="@/assets/pay.jpg" alt="pay" height="200"/>
                        </el-descriptions-item>
                    </el-descriptions>
                </div>
            </div>
        </el-card>
    </div>
</template>
<script>
export default {
    components:{},
    setup(props) {
        return { }
    }
}
</script>
<style lang="stylus">
.about-wrap{
    .el-descriptions__table .el-descriptions__label{
        display:none;
    }
}
</style>
<style lang="stylus" scoped>
.about-wrap{
    font-size:1.3rem;
    box-sizing: border-box;

    .inner{
        padding:2rem;
        a{margin-right:1rem;}
    }
}
</style>