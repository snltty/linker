<template>
    <el-table-column prop="tuntap" :label="$t('home.tuntap')" width="160">
        <template #header>
           <a href="javascript:;" class="a-line" @click="handleShowLease">{{$t('home.tuntap')}}</a>
        </template>
        <template #default="scope">
            <div v-if="scope.row.hook_tuntap">
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