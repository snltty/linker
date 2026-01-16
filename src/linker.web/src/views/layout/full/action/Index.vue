<template>
    <div class="action-wrap h-100">
        <Action :machineId="state.machineId"></Action>
    </div>
</template>
<script>
import { reactive } from 'vue'
import Action from '../../../components/action/Action.vue';
export default {
    props:['machineId'],
    components:{Action},
    setup(props) {
        const state = reactive({
            machineId: props.machineId,
        });
        return { state }
    }
}
</script>
<style lang="stylus" scoped>
.action-wrap{
    font-size:1.3rem;
    padding:1rem;
    box-sizing: border-box;
}
</style>