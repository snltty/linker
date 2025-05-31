<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的唤醒`" top="1vh" width="760">
        <div>
            <Wakeup :machineId="state.machineId"></Wakeup>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Wakeup from '../wakeup/Wakeup.vue'
import { useOper } from './oper';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Wakeup
    },
    setup(props, { emit }) {
        const oper = useOper();
        
        const state = reactive({
            show: true,
            machineId: oper.value.device.id,
            machineName: oper.value.device.name
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });
        return {
            state
        }
    }
}
</script>
<style lang="stylus" scoped>

</style>