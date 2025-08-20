<template>
    <el-table-column prop="tuntap" :label="tuntap.show?$t('home.tuntap'):''" width="160">
        <template #header>
           <a v-if="tuntap.show" href="javascript:;" class="a-line" @click="handleShowLease">{{$t('home.tuntap')}}</a>
        </template>
        <template #default="scope">
            <div v-if="tuntap.show && tuntap.list[scope.row.MachineId]">
                <TuntapShow :config="true" :item="scope.row"></TuntapShow>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { useTuntap } from './tuntap';
import TuntapShow from '../tuntap/TuntapShow.vue';
export default {
    components:{TuntapShow},
    setup(props, { emit }) {

        const tuntap = useTuntap();

        const handleShowLease = ()=>{
            tuntap.value.showLease = true;
        }
       
        return {
            tuntap,handleShowLease
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>