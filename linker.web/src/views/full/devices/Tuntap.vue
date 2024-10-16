<template>
    <el-table-column prop="tuntap" label="虚拟网卡" width="160">
        <template #header>
           <a href="javascript:;" class="a-line">虚拟网卡</a>
        </template>
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

        const handleTuntapIP = (_tuntap) => {
            emit('edit',_tuntap);
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