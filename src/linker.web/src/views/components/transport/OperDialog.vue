<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="state.title" top="1vh" width="98%">
        <div>
            <Transport :machineId="state.machineId"></Transport>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Transport from '../transport/Transport.vue'
import { useTransport } from './transport';
import { injectGlobalData } from '@/provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Transport
    },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const transport = useTransport();

        const isSelf = globalData.value.config.Client.Id ==  transport.value.device.id || !transport.value.device.id;

        const state = reactive({
            show: true,
            machineId: transport.value.device.id,
            title:isSelf? `[${transport.value.device.name}]上的隧道协议` : `本机与[${transport.value.device.name}]之间的隧道协议`,
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