<template>
    <el-table-column prop="tunnel" :label="$t('home.tunnel')" width="86">
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
                    :title="$t('home.holeText')"
                    @click="handleTunnel(tunnel.list[scope.row.MachineId],scope.row)">
                        <span>{{$t('home.jump')}}:{{tunnel.list[scope.row.MachineId].RouteLevel}}+{{tunnel.list[scope.row.MachineId].RouteLevelPlus}}</span>
                    </a>
                </div>
            </template>
        </template>
    </el-table-column>
</template>
<script>
import { useTunnel } from './tunnel';
import { useConnections,useForwardConnections,useSocks5Connections,useTuntapConnections } from '../connection/connections';
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import {useI18n} from 'vue-i18n';

export default {
    emits: ['edit','refresh'],
    setup(props, { emit }) {

        const t = useI18n();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);
        const hasTunnelChangeSelf = computed(()=>globalData.value.hasAccess('TunnelChangeSelf')); 
        const hasTunnelChangeOther = computed(()=>globalData.value.hasAccess('TunnelChangeOther')); 

        const tunnel = useTunnel();
        const connections = useConnections();
        const forwardConnections = useForwardConnections();
        const tuntapConnections = useTuntapConnections();
        const socks5Connections = useSocks5Connections();

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
            'jdcom':'jdcom.svg',
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
       
        const handleTunnel = (_tunnel,row) => {
            if(machineId.value === _tunnel.MachineId){
                if(!hasTunnelChangeSelf.value){
                    ElMessage.success(t('common.access'));
                return;
            }
            }else{
                if(!hasTunnelChangeOther.value){
                    ElMessage.success(t('common.access'));
                return;
            }
            }
            _tunnel.device = row;
            tunnel.value.current = _tunnel;
            tunnel.value.showEdit = true;
        }
        const handleTunnelRefresh = ()=>{
            emit('refresh');
        }
        const handleConnections = (row)=>{
            connections.value.current = row.MachineId;
            connections.value.currentName = row.MachineName;
            connections.value.showEdit = true;
        }
       
        return {
            tunnel, handleTunnel,handleTunnelRefresh,
            connectionCount,handleConnections,netImg,natMap
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
html.dark img.system,html.dark span.nat{border-color:#575c61;}

span.nat{display:inline-block;padding:0 .2rem;margin-right:0;font-family: fantasy;}
</style>