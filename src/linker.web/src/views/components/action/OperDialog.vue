<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的验证参数`" top="1vh" width="98%">
        <div>
            <Action :machineId="state.machineId"></Action>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch } from 'vue';
import Action from '../action/Action.vue'
import { useAction } from './action';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {
        Action
    },
    setup(props, { emit }) {
        const action = useAction();
        const state = reactive({
            show: true,
            machineId: action.value.device.id,
            machineName: action.value.device.name
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