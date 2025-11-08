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
        const show = computed(()=>globalData.value.isPhone);

        return {show}
    }
}
</script>

<style lang="stylus" scoped>
</style>