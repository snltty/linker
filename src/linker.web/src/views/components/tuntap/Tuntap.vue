<template>
    <el-table-column prop="tuntap" :label="$t('home.tuntap')" width="160">
        <template #header>
           <a href="javascript:;" class="a-line" @click="handleShowLease">{{$t('home.tuntap')}}</a>
        </template>
        <template #default="scope">
            <div>
                <template v-if="scope.row.hook_tuntap">
                    <TuntapShow :config="true" :item="scope.row"></TuntapShow>
                </template>
                <template v-else-if="!scope.row.hook_tuntap_load">
                    <el-skeleton animated>
                        <template #template>
                            <el-skeleton-item variant="text" style="width: 60%;" />
                            <el-skeleton-item variant="text" style="width: 20%;margin-left:20%" />
                            <el-skeleton-item variant="text" style="width: 70%" />
                        </template>
                    </el-skeleton>
                </template>
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