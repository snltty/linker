<template>
    <div class="transport-wrap">
       <Transport :machineId="state.machineId" :height="state.height"></Transport>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import Transport from './Transport.vue'
import { injectGlobalData } from '@/provide';
export default {
    props:['machineId'],
    components:{Transport},
    setup(props) {   
        const globalData = injectGlobalData();
        const state = reactive({
            machineId:props.machineId,
            height:computed(()=>globalData.value.height-20)
        })
        return {state}
    }
}
</script>
<style lang="stylus" scoped>
.transport-wrap{
    padding:1rem
}
</style>