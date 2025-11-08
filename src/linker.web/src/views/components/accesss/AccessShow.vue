<template>
    <slot v-if="show"></slot>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
export default {
    props:['value'],
    setup (props) {
        
        const globalData = injectGlobalData();
        const show = computed(()=>props.value.split(',').filter(c=>globalData.value.hasAccess(c)).length > 0 );

        return {show}
    }
}
</script>

<style lang="stylus" scoped>
</style>