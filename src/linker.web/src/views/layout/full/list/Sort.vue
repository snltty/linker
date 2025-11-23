<template>
   <el-table border style="width: 100%" height="32px" size="small" @sort-change="handleSortChange" class="table-sort">
        <el-table-column prop="MachineId" :label="$t('home.device')" width="90" sortable="custom" ></el-table-column>
        <el-table-column prop="Version" :label="$t('home.version')" width="90" sortable="custom"></el-table-column>
        <el-table-column prop="tunnel" :label="$t('home.tunnel')" width="86" sortable="custom"></el-table-column>
        <el-table-column prop="tuntap" :label="$t('home.tuntapIP')" width="160" sortable="custom"></el-table-column>
        <el-table-column prop="socks5" :label="$t('home.proxy')" width="160" sortable="custom"></el-table-column>
        <el-table-column label="..." fixed="right"  min-width="110">
            <template #header>
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