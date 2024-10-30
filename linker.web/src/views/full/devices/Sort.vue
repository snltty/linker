<template>
   <el-table border style="width: 100%" height="32px" size="small" @sort-change="handleSortChange" class="table-sort">
        <el-table-column prop="MachineId" label="设备名" width="110" sortable="custom" ></el-table-column>
        <el-table-column prop="Version" label="版本" width="110" sortable="custom"></el-table-column>
        <el-table-column prop="tunnel" label="网关" width="70" sortable="custom"></el-table-column>
        <el-table-column v-if="tuntap.show" prop="tuntap" label="网卡IP" width="160" sortable="custom"></el-table-column>
        <el-table-column v-if="socks5.show" prop="socks5" label="代理转发" width="160" sortable="custom"></el-table-column>
        <el-table-column label="columns" fixed="right">
            <template #header>
                <el-checkbox v-model="tuntap.show" @change="handleTuntapShow" size="small" style="margin-right:1rem">网卡</el-checkbox> 
                <el-checkbox v-model="socks5.show" @change="handleSocks5Show" size="small" style="margin-right:1rem">代理</el-checkbox> 
                <el-checkbox v-model="forward.show" @change="handleForwardShow" size="small" style="margin-right:0rem">转发</el-checkbox> 
            </template>
        </el-table-column>
    </el-table>
</template>

<script>
import { useForward } from './forward';
import { useSocks5 } from './socks5';
import { useTuntap } from './tuntap';

export default {
    emits: ['sort'],
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
</style>