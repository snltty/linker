<template>
    <el-dialog append-to=".app-wrap" v-model="state.show" :title="state.title" top="1vh" width="80rem">
        <div>
            <Transport :machineId="state.machineId" :machineName="state.machineName"></Transport>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Transport from '../transport/Transport.vue'
import { useTransport } from './transport';
import { injectGlobalData } from '@/provide';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Transport
    },
    setup(props, { emit }) {

        const {t} = useI18n();
        const globalData = injectGlobalData();
        const transport = useTransport();

        const isSelf = globalData.value.config.Client.Id ==  transport.value.device.id || !transport.value.device.id;

        const state = reactive({
            show: true,
            machineId: transport.value.device.id,
            machineName: transport.value.device.name,
            title:isSelf ? t('tunnel.title',[transport.value.device.name]): t('tunnel.title1',[transport.value.device.name]),
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