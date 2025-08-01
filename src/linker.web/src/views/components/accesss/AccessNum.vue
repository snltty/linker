<template>
    <span class="accesss">😀🫥{{accessHasLength}}/{{ accessLength }}</span>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import { useAccess } from './access';

export default {
    props:['item'],
    setup (props) {
        
        const globalData = injectGlobalData();
        const accesss = useAccess();
        const accessHasLength = computed(()=>{
            if(accesss.value.list[props.item.MachineId])
                return accesss.value.list[props.item.MachineId].split('').filter(c=>c==='1').length;
            return 0
        });
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