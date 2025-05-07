<template>
    <el-table-column prop="socks5" :label="socks5.show?'代理转发':''" width="160">
        <template #default="scope">
            <div v-if="socks5.show && socks5.list[scope.row.MachineId]">
                <Socks5Show :config="true" :item="scope.row" @edit="handleSocks5" @refresh="handleSocks5Refresh"></Socks5Show>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { useSocks5 } from './socks5';
import Socks5Show from './Socks5Show.vue';
export default {
    emits: ['edit','refresh'],
    components:{Socks5Show},
    setup(props, { emit }) {

        const socks5 = useSocks5();

        const handleSocks5 = (_socks5) => {
            emit('edit',_socks5);
        }
        const handleSocks5Refresh = ()=>{
            emit('refresh');
        }

       
        return {
            socks5, handleSocks5,handleSocks5Refresh
        }
    }
}
</script>
<style lang="stylus" scoped>
</style>