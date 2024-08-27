<template>
    <div class="head-wrap flex">
        <div class="run">
            <HeadRun></HeadRun>
        </div>
        <div class="tools flex-1">
            <HeadSetting></HeadSetting>
        </div>
    </div>
</template>

<script>
import HeadRun from './HeadRun.vue';
import HeadSetting from './HeadSetting.vue';
export default {
    components:{HeadRun,HeadSetting},
    setup () {
        
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.head-wrap{
    background-color:#fff;
    padding:1rem;
    border-bottom:1px solid #ddd;
    box-shadow:1px 2px 3px rgba(0,0,0,.05);
}
</style>