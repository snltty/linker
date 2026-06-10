<template>
    <el-dialog append-to=".app-wrap" v-model="state.show" :title="$t('wakeup.title',[state.machineName])"
     top="1vh" width="80rem" style="height:80vh" class="wakeup-dialog">
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