<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的防火墙`" 
    top="1vh" width="98%" style="height:80vh" class="firewall-dialog">
        <div class="h-100">
            <Firewall :machineId="state.machineId"></Firewall>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Firewall from '../firewall/Firewall.vue'
import { useFirewall } from './firewall';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Firewall
    },
    setup(props, { emit }) {
        const firewall = useFirewall();
        
        const state = reactive({
            show: true,
            machineId: firewall.value.device.id,
            machineName: firewall.value.device.name
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
<style lang="stylus">
.firewall-dialog{
    .el-dialog__body{
        height: calc(100% - 4.5rem);
    }
}
</style>
<style lang="stylus" scoped>

</style>