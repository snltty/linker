<template>
    <el-table-column prop="tuntap" label="隧道" width="90">
        <template #header>
            <div class="flex">
                <span class="flex-1">隧道</span>
                <el-button size="small" @click="handleTunnelRefresh"><el-icon><Refresh /></el-icon></el-button>
            </div>
        </template>
        <template #default="scope">
            <div v-if="tunnel.list[scope.row.MachineId]">
                <p>
                    <a href="javascript:;" class="a-line" @click="handleTunnel(tunnel.list[scope.row.MachineId])">
                    <span>网关 : {{tunnel.list[scope.row.MachineId].RouteLevel}} + {{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
                    </a>
                </p>
            </div> 
            <p v-if="connections.list[scope.row.MachineId]">
                <a href="javascript:;" class="a-line" @click="handleConnections(scope.row.MachineId)">
                    <span>连接数 : {{connections.list[scope.row.MachineId].length}}</span>
                    </a>
            </p>
        </template>
    </el-table-column>
</template>
<script>
import { inject, reactive } from 'vue';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const tunnel = inject('tunnel');
        const connections = inject('connections');
       
        const handleTunnel = (tunnel) => {
            emit('edit',tunnel);
        }
        const handleTunnelRefresh = ()=>{
            emit('refresh');
        }
        const handleConnections = (machineId)=>{
            emit('connections',machineId);
        }
       
        return {
            tunnel, handleTunnel,handleTunnelRefresh,
            connections,handleConnections
        }
    }
}
</script>
<style lang="stylus" scoped>
.green{color:green;}
.el-switch.is-disabled{opacity :1;}
</style>