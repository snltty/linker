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
            <p>
                <a href="javascript:;" class="a-line" @click="handleConnections(scope.row.MachineId)">
                    <span>连接数 : {{connectionCount(scope.row.MachineId)}}</span>
                    </a>
            </p>
        </template>
    </el-table-column>
</template>
<script>
import { computed, inject, reactive } from 'vue';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const tunnel = inject('tunnel');

        const forwardConnections = inject('forward-connections');
        const tuntapConnections = inject('tuntap-connections');

        const connectionCount = (machineId)=>{
                return [
                    forwardConnections.value.list[machineId],
                    tuntapConnections.value.list[machineId],
                ].filter(c=>!!c).length;
        };
       
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
            connectionCount,handleConnections
        }
    }
}
</script>
<style lang="stylus" scoped>
.green{color:green;}
.el-switch.is-disabled{opacity :1;}
</style>