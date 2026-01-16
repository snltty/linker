<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的唤醒`"
     top="1vh" width="98%" style="height:80vh" class="wakeup-dialog">
        <div class="h-100">
            <Wakeup :machineId="state.machineId"></Wakeup>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Wakeup from '../wakeup/Wakeup.vue'
import { useWakeup } from './wakeup';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Wakeup
    },
    setup(props, { emit }) {
        const wakeup = useWakeup();
        
        const state = reactive({
            show: true,
            machineId: wakeup.value.device.id,
            machineName: wakeup.value.device.name
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
.wakeup-dialog{
    .el-dialog__body{
        height: calc(100% - 4.5rem);
    }
}
</style>
<style lang="stylus" scoped>

</style>