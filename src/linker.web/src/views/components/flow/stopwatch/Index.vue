<template>
    <el-dialog :title="`${$t('status.messengerName')}`" class="options-center" top="1vh" destroy-on-close v-model="state.show" width="480">
        <div>
            <el-tabs type="border-card">
                <el-tab-pane :label="flow.device.name">
                    <Stopwatch :machineId="flow.device.id"></Stopwatch>
                </el-tab-pane>
                <el-tab-pane :label="$t('server.messenger')">
                    <Stopwatch :machineId="''"></Stopwatch>
                </el-tab-pane>
            </el-tabs>
        </div>
    </el-dialog>
</template>

<script>
import { reactive, watch } from 'vue';
import { useFlow } from '../flow';
import Stopwatch from './Stopwatch.vue';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{Stopwatch},
    setup (props,{emit}) {
        
        const flow = useFlow();
        const state = reactive({
            show:true,
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        return {
            state,flow
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>