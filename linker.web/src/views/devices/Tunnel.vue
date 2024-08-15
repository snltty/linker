<template>
    <el-table-column prop="tunnel" label="隧道" width="90">
        <template #default="scope">
            <div v-if="tunnel.list[scope.row.MachineId]">
                <a href="javascript:;" class="a-line" 
                :class="{yellow:tunnel.list[scope.row.MachineId].NeedReboot}" 
                :title="tunnel.list[scope.row.MachineId].NeedReboot?'需要重启':''"
                @click="handleTunnel(tunnel.list[scope.row.MachineId])">
                    <span>网关 : {{tunnel.list[scope.row.MachineId].RouteLevel}} + {{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
                </a>
            </div> 
            <div>
                <a href="javascript:;" class="a-line" :class="{green:connectionCount(scope.row.MachineId)>0}" @click="handleConnections(scope.row.MachineId)">
                    连接数 : <span>{{connectionCount(scope.row.MachineId)}}</span>
                </a>
            </div>
        </template>
    </el-table-column>
</template>
<script>
import { useTunnel } from './tunnel';
import { useConnections,useForwardConnections,useTuntapConnections } from './connections';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const tunnel = useTunnel();
        const connections = useConnections();
        const forwardConnections = useForwardConnections();
        const tuntapConnections = useTuntapConnections();

        const connectionCount = (machineId)=>{
                return [
                    forwardConnections.value.list[machineId],
                    tuntapConnections.value.list[machineId],
                ].filter(c=>!!c && c.Connected).length;
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

.el-switch.is-disabled{opacity :1;}

.green{font-weight:bold;}
</style>