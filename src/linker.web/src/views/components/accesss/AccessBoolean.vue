<template>
    <slot :values="values"></slot>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';

export default {
    props: ['value'],
    setup (props) {
        
        const globalData = injectGlobalData();
        const values = computed(()=>props.value.split(',').reduce((json,item,index)=>{
            json[item] = globalData.value.hasAccess(item);
            return json;
        },{}) );

        return {values}
    }
}
</script>

<style lang="stylus" scoped>
</style>