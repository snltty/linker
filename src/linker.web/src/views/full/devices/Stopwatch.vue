<template>
    <el-dialog :title="`${flow.device.name}->${$t('status.messengerName')}`" class="options-center" top="1vh" destroy-on-close v-model="state.show" width="480">
        <div>
            <el-table :data="state.list" stripe border size="small" width="100%" height="60vh">
                <el-table-column prop="id" label="id" width="200"></el-table-column>
                <el-table-column prop="request" label="request" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.request }}ms / {{ scope.row.requestMax }}ms</span>
                    </template>
                </el-table-column>
                <el-table-column prop="response" label="response" sortable>
                    <template #default="scope">
                        <span>{{ scope.row.response }}ms / {{ scope.row.responseMax }}ms</span>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>

<script>
import {  getStopwatch } from '@/apis/flow';
import { onMounted, onUnmounted, reactive, watch } from 'vue';
import { useFlow } from './flow';
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    setup (props,{emit}) {
        
        const {t} = useI18n();
        const flow = useFlow();
        const state = reactive({
            show:true,
            timer:0,
            list:[]
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const _getStopwatch = ()=>{
            clearTimeout(state.timer);
            getStopwatch(flow.value.device.id).then(res => {
                state.list = Object.keys(res).map(c=>{
                    return {
                        id:`${t(`status.messenger${c}`)}(${c})`,
                        request:(BigInt(res[c].SendtBytes) & (BigInt(0xffffffff))).toString(),
                        requestMax:(BigInt(res[c].SendtBytes)>>BigInt(32)).toString(),
                        response:(BigInt(res[c].ReceiveBytes) & (BigInt(0xffffffff))).toString(),
                        responseMax:(BigInt(res[c].ReceiveBytes)>>BigInt(32)).toString(),
                    }
                });
                state.timer = setTimeout(_getStopwatch,3000);
            }).catch((e)=>{
                state.timer = setTimeout(_getStopwatch,3000);
            });
        }

        onMounted(()=>{
            _getStopwatch();
        });
        onUnmounted(()=>{
            clearTimeout(state.timer);
        });
        

        return {
            state,flow
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>