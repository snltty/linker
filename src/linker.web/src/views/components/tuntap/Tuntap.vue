<template>
    <el-table-column prop="tuntap" :label="$t('home.tuntap')" width="160">
        <template #header>
           <a href="javascript:;" class="a-line" @click="handleShowLease">{{$t('home.tuntap')}}</a>
        </template>
        <template #default="scope">
            <div v-if="scope.row ">
                <template v-if="scope.row.hook_tuntap">
                    <div class="skeleton-animation" :style="`animation-delay:${scope.row.animationDelay}ms`">
                        <TuntapShow :config="true" :item="scope.row"></TuntapShow>
                    </div>
                </template>
                <template v-else-if="!scope.row.hook_tuntap_load">
                    <div class="skeleton-animation">
                        <el-skeleton animated>
                            <template #template>
                                <el-skeleton-item variant="text" style="width: 60%;" />
                                <el-skeleton-item variant="text" style="width: 20%;margin-left:20%" />
                                <el-skeleton-item variant="text" style="width: 70%" />
                            </template>
                        </el-skeleton>
                    </div>
                </template>
            </div> 
            <div class="device-remark"></div>
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