<template>
    <el-table-column prop="tunnel" label="网络" width="76">
        <template #default="scope">
            <template v-if="tunnel.list[scope.row.MachineId]">
                <div>
                    <template v-if="tunnel.list[scope.row.MachineId].Net.CountryCode">
                        <img 
                        :title="`${tunnel.list[scope.row.MachineId].Net.Country}(${tunnel.list[scope.row.MachineId].Net.CountryCode})、${tunnel.list[scope.row.MachineId].Net.RegionName}(${tunnel.list[scope.row.MachineId].Net.Region})、${tunnel.list[scope.row.MachineId].Net.City}`" 
                        class="system" 
                        :src="`https://unpkg.com/flag-icons@7.2.3/flags/4x3/${tunnel.list[scope.row.MachineId].Net.CountryCode.toLowerCase()}.svg`" />
                    </template>
                    <template v-else>
                        <img title="?" class="system" src="/system.svg" />
                    </template>
                    <template v-if="tunnel.list[scope.row.MachineId].Net.Isp">
                        <img 
                        :title="`${tunnel.list[scope.row.MachineId].Net.Isp}、${tunnel.list[scope.row.MachineId].Net.Org}、${tunnel.list[scope.row.MachineId].Net.As}`" 
                        class="system" :src="netImg(tunnel.list[scope.row.MachineId].Net)" />
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
                        <span>跳点 : {{tunnel.list[scope.row.MachineId].RouteLevel}}+{{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
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
                '调整网关层级有助于打洞成功',
                `${item.HostName}`,
                item.Lans.filter(c=>c.Ips.length > 0).map(c=>`\t【${c.Mac||'00-00-00-00-00-00'}】${c.Name}\r\n\t\t${c.Ips.join('\r\n\t\t')}`).join('\r\n'),
                `跳跃点\r\n\t${item.Routes.join('\r\n\t')}`
            ]

            return item.NeedReboot
            ?'需要重启'
            :texts.join('\r\n');
        }

        const imgMap = {
            'chinanet':'chinanet.svg',
            'china telecom':'chinanet.svg',
            'china unicom':'chinaunicom.svg',
            'china mobile':'chinamobile.svg',
            'huawei':'huawei.svg',
            'amazon':'amazon.svg',
            'aliyun':'aliyun.svg',
        }
        const netImg = (item)=>{
            const isp = item.Isp.toLowerCase();
            const org = item.Org.toLowerCase();
            const as = item.As.toLowerCase();
            for(let j in imgMap){
                if(isp.indexOf(j) > -1 || org.indexOf(j) > -1 || as.indexOf(j) > -1){
                    return `./${imgMap[j]}`;
                }
            }
            return `./system.svg`;
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
            connectionCount,handleConnections,title,netImg
        }
    }
}
</script>
<style lang="stylus" scoped>

.el-switch.is-disabled{opacity :1;}

.green{font-weight:bold;}

img.system{
    height:1.4rem;
    margin-right:.4rem
    border: 1px solid #eee;
}
</style>