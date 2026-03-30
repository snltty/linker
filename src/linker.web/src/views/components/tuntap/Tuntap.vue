<template>
    <el-table-column prop="tuntap" :label="$t('home.tuntap')" width="160">
        <template #header>
            <div class="flex">
                    <a href="javascript:;" class="a-line" @click="handleShowLease">{{$t('home.tuntap')}}</a>
                    <span class="w-1"></span>
                    <el-select size="small" class="flex-1" v-model="tuntap.network" @change="handleChange">
                        <template v-for="item in tuntap.networks">
                            <el-option :value="item.value" :label="item.label"></el-option>
                        </template>
                    </el-select>
            </div>
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
                                <el-skeleton-item variant="text" class="w-60-"/>
                                <el-skeleton-item variant="text" class="w-20- mgl-20-"/>
                                <el-skeleton-item variant="text" class="w-70-"/>
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
        const handleChange = (val)=>{
            tuntap.value.network = val;
            emit('refresh')
        }
       
        return {
            tuntap,handleShowLease,handleChange
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>