<template>
    <div class="transport-wrap relative h-100">
       <div class="relative h-100">
            <div class="absolute">
                <Transport :machineId="state.machineId"></Transport>
            </div>
       </div>
    </div>
</template>
<script>
import { computed, reactive } from 'vue';
import Transport from '../../../components/transport/Transport.vue'
import { injectGlobalData } from '@/provide';
export default {
    props:['machineId'],
    components:{Transport},
    setup(props) {   
        const globalData = injectGlobalData();
        const state = reactive({
            machineId:props.machineId
        })
        return {state}
    }
}
</script>
<style lang="stylus" scoped>
.transport-wrap{
    padding:1rem;
    box-sizing: border-box;

}
</style>