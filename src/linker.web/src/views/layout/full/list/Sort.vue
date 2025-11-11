<template>
   <el-table border style="width: 100%" height="32px" size="small" @sort-change="handleSortChange" class="table-sort">
        <el-table-column prop="MachineId" :label="$t('home.device')" width="90" sortable="custom" ></el-table-column>
        <el-table-column prop="Version" :label="$t('home.version')" width="90" sortable="custom"></el-table-column>
        <el-table-column prop="tunnel" :label="$t('home.tunnel')" width="86" sortable="custom"></el-table-column>
        <el-table-column v-if="tuntap.show" prop="tuntap" :label="$t('home.tuntapIP')" width="160" sortable="custom"></el-table-column>
        <el-table-column v-if="socks5.show" prop="socks5" :label="$t('home.proxy')" width="160" sortable="custom"></el-table-column>
        <el-table-column label="columns" fixed="right">
            <template #header>
                <el-dropdown class="show-columns">
                    <span class="el-dropdown-link">{{$t('home.showItems')}}<el-icon><ArrowDownBold /></el-icon></span>
                    <template #dropdown>
                    <el-dropdown-menu>
                        <el-dropdown-item><el-checkbox v-model="tuntap.show" @change="handleTuntapShow" size="small" style="margin-right:1rem">{{$t('home.tuntap')}}</el-checkbox> </el-dropdown-item>
                        <el-dropdown-item><el-checkbox v-model="socks5.show" @change="handleSocks5Show" size="small" style="margin-right:1rem">{{$t('home.proxy')}}</el-checkbox> </el-dropdown-item>
                        <el-dropdown-item><el-checkbox v-model="forward.show" @change="handleForwardShow" size="small" style="margin-right:0rem">{{$t('home.forward')}}</el-checkbox></el-dropdown-item>
                    </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </template>
        </el-table-column>
    </el-table>
</template>

<script>
import { useForward } from '../../../components/forward/forward';
import { useSocks5 } from '../../../components/socks5/socks5';
import { useTuntap } from '../../../components/tuntap/tuntap';
import { ArrowDownBold } from '@element-plus/icons-vue';

export default {
    emits: ['sort'],
    components: { ArrowDownBold },
    setup (props, { emit }) {

        const tuntap = useTuntap();
        tuntap.value.show = localStorage.getItem('tuntap.show')!='false';
        const socks5 = useSocks5();
        socks5.value.show = localStorage.getItem('socks5.show')!='false';
        const forward = useForward();
        forward.value.show = localStorage.getItem('forward.show')!='false';
        
        const handleSortChange = (row)=>{
            emit('sort',row);
        }
        const handleTuntapShow = ()=>{
            localStorage.setItem('tuntap.show',tuntap.value.show);
        }
        const handleSocks5Show = ()=>{
            localStorage.setItem('socks5.show',socks5.value.show);
        }
        const handleForwardShow = ()=>{
            localStorage.setItem('forward.show',forward.value.show);
        }
        

        return {tuntap,socks5,forward,handleSortChange,handleTuntapShow,handleSocks5Show,handleForwardShow}
    }
}
</script>

<style lang="stylus" scoped>
.table-sort 
{
    th{border-bottom:0}
}
.show-columns{
    vertical-align:middle;
    font-size:1.2rem;
    .el-icon{
        vertical-align:bottom;
        font-size:1.2rem;
    }
}
</style>