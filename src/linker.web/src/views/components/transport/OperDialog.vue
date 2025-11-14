<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`本机与[${state.machineName}]之间的打洞协议`" top="1vh" width="98%">
        <div>
            <Transport :machineId="state.machineId"></Transport>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Transport from '../transport/Transport.vue'
import { useTransport } from './transport';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Transport
    },
    setup(props, { emit }) {
        const transport = useTransport();
        
        const state = reactive({
            show: true,
            machineId: transport.value.device.id,
            machineName: transport.value.device.name
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