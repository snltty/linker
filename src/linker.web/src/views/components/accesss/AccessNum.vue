<template>
    <span class="accesss">😀🫥{{accessHasLength}}/{{ accessLength }}</span>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';

export default {
    props:['item'],
    setup (props) {
        
        const globalData = injectGlobalData();
        const accessHasLength = computed(()=>(props.item.hook_accesss || '').split('').filter(c=>c==='1').length);
        const accessLength = computed(()=>{
            return Object.keys(globalData.value.config.Client.Accesss).length;
        });

        return {accessHasLength,accessLength}
    }
}
</script>

<style lang="stylus" scoped>
.accesss{
    margin-left:.4rem
}
</style>