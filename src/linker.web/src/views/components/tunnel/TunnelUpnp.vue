<template>
     <el-dialog append-to=".app-wrap" v-model="state.show" :close-on-click-modal="false" :title="$t('network.upnp.title',[state.machineName])" width="80rem" top="2vh">
        <div>
            <el-tabs type="border-card">
                <el-tab-pane :label="$t('network.upnp.myadd')">
                    <TunnelUpnpLocal :deviceTypes="state.deviceTypes" :protocolTypes="state.protocolTypes" :machineId="state.machineId"></TunnelUpnpLocal>
                </el-tab-pane>
                <el-tab-pane :label="$t('network.upnp.online')">
                    <TunnelUpnpRemote :deviceTypes="state.deviceTypes" :protocolTypes="state.protocolTypes" :machineId="state.machineId"></TunnelUpnpRemote>
                </el-tab-pane>
            </el-tabs>
        </div>
    </el-dialog>
</template>
<script>
import {reactive, watch } from 'vue';
import {useTunnel } from './tunnel';
import TunnelUpnpLocal from './TunnelUpnpLocal.vue';
import TunnelUpnpRemote from './TunnelUpnpRemote.vue';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {TunnelUpnpLocal,TunnelUpnpRemote},
    setup(props, { emit }) {

        const tunnel = useTunnel();
        const state = reactive({
            tab: '',
            show: true,
            machineName: tunnel.value.current.device.MachineName,
            machineId: tunnel.value.current.device.MachineId,
            deviceTypes:{1:'UPNP',2:'PMP',4:'PCP',255:'Any'},
            protocolTypes:{6:'TCP',17:'UDP'},
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
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