<template>
    <el-table-column prop="tunnel" label="隧道" width="70">
        <template #default="scope">
            <div v-if="tunnel.list[scope.row.MachineId]">
                <a href="javascript:;" class="a-line" 
                :class="{yellow:tunnel.list[scope.row.MachineId].NeedReboot}" 
                :title="tunnel.list[scope.row.MachineId].NeedReboot?'需要重启':'调整网关层级有助于打洞成功'"
                @click="handleTunnel(tunnel.list[scope.row.MachineId],scope.row)">
                    <span>网关:{{tunnel.list[scope.row.MachineId].RouteLevel}}+{{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
                </a>
            </div> 
            <div>
                <a href="javascript:;" title="与此设备的隧道连接" class="a-line" :class="{green:connectionCount(scope.row.MachineId)>0}" @click="handleConnections(scope.row)">
                    连接:<span>{{connectionCount(scope.row.MachineId)}}</span>
                </a>
            </div>
        </template>
    </el-table-column>
</template>
<script>
import { useTunnel } from './tunnel';
import { useConnections,useForwardConnections,useSocks5Connections,useTuntapConnections } from './connections';
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const hasTunnelChangeSelf = computed(()=>globalData.value.hasAccess('TunnelChangeSelf')); 
        const hasTunnelChangeOther = computed(()=>globalData.value.hasAccess('TunnelChangeOther')); 

        const tunnel = useTunnel();
        const connections = useConnections();
        const forwardConnections = useForwardConnections();
        const tuntapConnections = useTuntapConnections();
        const socks5Connections = useSocks5Connections();

        const connectionCount = (machineId)=>{
                return [
                    forwardConnections.value.list[machineId],
                    tuntapConnections.value.list[machineId],
                    socks5Connections.value.list[machineId],
                ].filter(c=>!!c && c.Connected).length;
        };
       
        const handleTunnel = (tunnel,row) => {
            if(machineId.value === tunnel.MachineId){
                if(!hasTunnelChangeSelf.value){
                    return;
                }
            }else{
                if(!hasTunnelChangeOther.value){
                    return;
                }
            }
            tunnel.device = row;
            emit('edit',tunnel);
        }
        const handleTunnelRefresh = ()=>{
            emit('refresh');
        }
        const handleConnections = (row)=>{
            emit('connections',row);
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