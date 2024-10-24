<template>
    <el-table-column prop="socks5" label="socks5代理" width="160">
        <template #default="scope">
            <div>
                <div class="flex">
                    <div class="flex-1">
                        <a href="javascript:;" class="a-line" title="socks5代理">
                            <strong class="gateway">socks5://*:1805</strong>
                        </a>
                    </div>
                    <el-switch size="small" inline-prompt active-text="😀" inactive-text="😣"/>
                </div>
                <div>1111</div>
            </div>
        </template>
    </el-table-column>
</template>
<script>
import { injectGlobalData } from '@/provide';
export default {
    emits: ['edit','sedit'],
    setup(props, { emit }) {
        const globalData = injectGlobalData();
        return {}
    }
}
</script>
<style lang="stylus" scoped>
.gateway{
    background:linear-gradient(90deg, #c5b260, #858585, #c5b260, #858585);
    -webkit-background-clip:text;
    -webkit-text-fill-color:hsla(0,0%,100%,0);
    &.green{
        background:linear-gradient(90deg, #e4bb10, #008000, #e4bb10, #008000);
        -webkit-background-clip:text;
        -webkit-text-fill-color:hsla(0,0%,100%,0);
    }
}
</style>