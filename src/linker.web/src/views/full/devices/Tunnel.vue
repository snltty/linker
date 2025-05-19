<template>
    <el-table-column prop="tunnel" label="网络" width="76">
        <template #default="scope">
            <template v-if="tunnel.list[scope.row.MachineId]">
                <div>
                    <template v-if="tunnel.list[scope.row.MachineId].Net.CountryCode">
                        <img 
                        :title="`${tunnel.list[scope.row.MachineId].Net.CountryCode}、${tunnel.list[scope.row.MachineId].Net.City}`" 
                        class="system" 
                        :src="`https://unpkg.com/flag-icons@7.2.3/flags/4x3/${tunnel.list[scope.row.MachineId].Net.CountryCode.toLowerCase()}.svg`" />
                    </template>
                    <template v-else>
                        <img title="?" class="system" src="/system.svg" />
                    </template>
                    <template v-if="tunnel.list[scope.row.MachineId].Net.Isp">
                        <img 
                        :title="`${tunnel.list[scope.row.MachineId].Net.Isp}`" 
                        class="system" :src="netImg(tunnel.list[scope.row.MachineId].Net)" />
                    </template>
                    <template v-else>
                        <img title="?" class="system" src="/system.svg" />
                    </template>
                    <template v-if="tunnel.list[scope.row.MachineId].Net.Nat">
                        <span class="nat" :title="tunnel.list[scope.row.MachineId].Net.Nat">{{ natMap[tunnel.list[scope.row.MachineId].Net.Nat]  }}</span>
                    </template>
                    <template v-else>
                        <img title="?" class="system" src="/system.svg" />
                    </template>
                </div> 
                <div class="flex">
                    <a href="javascript:;" class="a-line" 
                    :class="{yellow:tunnel.list[scope.row.MachineId].NeedReboot}" 
                    :title="title(tunnel.list[scope.row.MachineId])"
                    @click="handleTunnel(tunnel.list[scope.row.MachineId],scope.row)">
                        <span>跳点:{{tunnel.list[scope.row.MachineId].RouteLevel}}+{{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
                    </a>
                </div>
            </template>
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

        const title = (item)=>{
            let texts = [
                '调整网关层级有助于打洞成功'
            ]
            return item.NeedReboot
            ?'需要重启'
            :texts.join('\r\n');
        }

        const imgMap = {
            'chinanet':'chinanet.svg',
            'china169':'chinanet.svg',
            'china telecom':'chinanet.svg',
            'china unicom':'chinaunicom.svg',
            'china mobile':'chinamobile.svg',
            'huawei':'huawei.svg',
            'amazon':'amazon.svg',
            'aliyun':'aliyun.svg',
            'alibaba':'aliyun.svg',
        }
        const regex = new RegExp(Object.keys(imgMap).map(item => `\\b${item}\\b`).join("|"));
        const netImg = (item)=>{
            const isp = item.Isp.toLowerCase();
            if(isp){
                const macth = isp.match(regex);
                if(macth){
                    return `./${imgMap[macth[0]]}`;
                }
            }
            return `./system.svg`;
        }

        const natMap = {
            "Unknown":'?',
            "UnsupportedServer":'?',
            "UdpBlocked":'?',
            "OpenInternet":'?',
            "SymmetricUdpFirewall":'?',
            "FullCone":'1',
            "RestrictedCone":'2',
            "PortRestrictedCone":'3',
            "Symmetric":'4',
        }

        const connectionCount = (machineId)=>{
                const length = [
                    forwardConnections.value.list[machineId],
                    tuntapConnections.value.list[machineId],
                    socks5Connections.value.list[machineId],
                ].filter(c=>!!c && c.Connected).length;
                return length;
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
            connectionCount,handleConnections,title,netImg,natMap
        }
    }
}
</script>
<style lang="stylus" scoped>

.el-switch.is-disabled{opacity :1;}

.green{font-weight:bold;}

img.system,span.nat{
    height:1.4rem;
    margin-right:.4rem
    border: 1px solid #eee;
    line-height:1.4rem;
    vertical-align:middle;
}
span.nat{display:inline-block;padding:0 .2rem;margin-right:0;font-family: fantasy;}
</style>