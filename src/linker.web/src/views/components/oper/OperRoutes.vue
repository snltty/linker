<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="`[${state.machineName}]上的路由`" top="1vh" >
        <div>
            <el-table :data="state.data" size="small" border height="500">
                <el-table-column property="Ip" label="IP"></el-table-column>
                <el-table-column property="Id" label="目标">
                    <template #default="scope">
                        <span>{{ state.names[scope.row.Id] }}</span>
                    </template>
                </el-table-column>
            </el-table>
        </div>
    </el-dialog>
</template>
<script>
import { reactive, watch,onMounted, onUnmounted } from 'vue';
import { getSignInNames } from '@/apis/signin';
import { getTuntapRoutes } from '@/apis/tuntap';
import { useOper } from './oper';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    setup(props, { emit }) {
        const oper = useOper();
        
        const state = reactive({
            show: true,
            machineId: oper.value.device.id,
            machineName: oper.value.device.name,
            data:[],
            names: {},
            timer: 0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                    emit('change')
                }, 300);
            }
        });

        onMounted(() => {
            getSignInNames().then((res)=>{
                state.names = res.reduce((json,value)=>{ json[value.MachineId]=value.MachineName; return json; },{});
            }).catch(()=>{});
            getTuntapRoutes(state.machineId).then((res)=>{
                state.data = Object.keys(res).map((value)=>{
                    return {
                        Ip: value,
                        Id: res[value]
                    }
                });
            }).catch(()=>{})
        });
        onUnmounted(() => {
        })

        return {
            state
        }
    }
}
</script>
<style lang="stylus" scoped>

</style>