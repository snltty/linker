<template>
    <Flow :config="true" :title="machineName"></Flow>
</template>
<script>
import { watch } from 'vue';
import Flow from '../flow/Index.vue'
import { useOper } from './oper';
import { useFlow } from '../flow/flow';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Flow},
    setup(props, { emit }) {
        const oper = useOper();
        const flow = useFlow();
        flow.value.machineId = oper.value.device.id;
        flow.value.count = true;

        watch(() => flow.value.count, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });
        return {
            machineName: oper.value.device.name
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>