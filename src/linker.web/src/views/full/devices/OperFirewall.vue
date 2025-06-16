<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的防火墙`" top="1vh" width="98%">
        <div>
            <Firewall :machineId="state.machineId"></Firewall>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Firewall from '../firewall/Firewall.vue'
import { useOper } from './oper';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Firewall
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