<template>
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
</template>
<script>
import { reactive, onMounted } from 'vue';
import { getSignInNames } from '@/apis/signin';
import { getTuntapRoutes } from '@/apis/tuntap';
import { useTuntap } from './tuntap';
export default {
    props: ['item'],
    setup(props) {

        const tuntap = useTuntap();
        const state = reactive({
            machineId:tuntap.value.current.device.MachineId,
            machineName: tuntap.value.current.device.MachineName,
            data:[],
            names: {}
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

        return {
            state
        }
    }
}
</script>
<style lang="stylus" scoped>

</style>