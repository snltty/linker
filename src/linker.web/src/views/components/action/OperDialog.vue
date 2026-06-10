<template>
    <el-dialog append-to=".app-wrap" v-model="state.show"  
    :title="$t('action.title',[state.machineName])" top="1vh" width="80" style="height:80vh" class="action-dialog">
        <div class="h-100">
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
<style lang="stylus">
.action-dialog{
    .el-dialog__body{
        height: calc(100% - 4.5rem);
    }
}
</style>
<style lang="stylus" scoped>
</style>