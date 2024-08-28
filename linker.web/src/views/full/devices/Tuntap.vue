<template>
    <el-table-column prop="tuntap" label="虚拟网卡" width="160">
        <template #default="scope">
            <div v-if="tuntap.list[scope.row.MachineId]">
                <TuntapShow :config="true" :item="scope.row" @edit="handleTuntapIP" @refresh="handleTuntapRefresh"></TuntapShow>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { useTuntap } from './tuntap';
import TuntapShow from './TuntapShow.vue';
export default {
    emits: ['edit','refresh'],
    components:{TuntapShow},
    setup(props, { emit }) {

        const tuntap = useTuntap();

        const handleTuntapIP = (tuntap) => {
            emit('edit',tuntap);
        }
        const handleTuntapRefresh = ()=>{
            emit('refresh');
        }
       
        return {
            tuntap, handleTuntapIP,handleTuntapRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>