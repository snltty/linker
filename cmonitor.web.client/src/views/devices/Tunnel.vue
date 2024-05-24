<template>
    <el-table-column prop="tuntap" label="隧道" width="90">
        <template #header>
            <div class="flex">
                <span class="flex-1">隧道</span>
                <el-button size="small" @click="handleTunnelRefresh"><el-icon><Refresh /></el-icon></el-button>
            </div>
        </template>
        <template #default="scope">
            <div v-if="data[scope.row.MachineName]">
                <a href="javascript:;" class="a-line" @click="handleTunnel(data[scope.row.MachineName])">
                    <span>网关 : {{data[scope.row.MachineName].RouteLevel}} + {{data[scope.row.MachineName].RouteLevelPlus}}</span>
                </a>
            </div> 
        </template>
    </el-table-column>
</template>
<script>
import { reactive } from 'vue';

export default {
    props: ['data'],
    emits: ['change','refresh'],
    setup(props, { emit }) {
        const state = reactive({});
       
        const handleTunnel = (tunnel) => {
            emit('edit',tunnel);
        }
        const handleTunnelRefresh = ()=>{
            emit('refresh');
        }
       
        return {
            data: props.data, state, handleTunnel,handleTunnelRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.green{color:green;}
.el-switch.is-disabled{opacity :1;}
</style>