<template>
    <Flow :config="true" :title="machineName"></Flow>
</template>
<script>
import { watch } from 'vue';
import Flow from '../flow/Index.vue'
import { useFlow } from './flow';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Flow},
    setup(props, { emit }) {
        const flow = useFlow();
        flow.value.machineId = flow.value.device.id;
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
            machineName: flow.value.device.name
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>