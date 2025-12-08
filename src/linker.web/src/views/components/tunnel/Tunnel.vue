<template>
    <AccessBoolean value="TunnelChangeSelf,TunnelChangeOther">
        <template #default="{values}">
            <el-table-column prop="tunnel" :label="$t('home.tunnel')" width="86">
                <template #default="scope">
                    <template v-if="scope.row && scope.row.hook_tunnel">
                        <div class="skeleton-animation" :style="`animation-delay:${scope.row.animationDelay}ms`">
                            <div>
                                <template v-if="scope.row.hook_tunnel.Net.CountryCode">
                                    <img 
                                    :title="`${scope.row.hook_tunnel.Net.CountryCode}、${scope.row.hook_tunnel.Net.City}`" 
                                    class="system" 
                                    :src="`https://unpkg.com/flag-icons@7.2.3/flags/4x3/${scope.row.hook_tunnel.Net.CountryCode.toLowerCase()}.svg`" />
                                </template>
                                <template v-else>
                                    <img title="?" class="system" src="/system.svg" />
                                </template>
                                <template v-if="scope.row.hook_tunnel.Net.Isp">
                                    <img 
                                    :title="`${scope.row.hook_tunnel.Net.Isp}`" 
                                    class="system" :src="netImg(scope.row.hook_tunnel.Net)" />
                                </template>
                                <template v-else>
                                    <img title="?" class="system" src="/system.svg" />
                                </template>
                                <template v-if="scope.row.hook_tunnel.Net.Nat">
                                    <span class="nat" :title="scope.row.hook_tunnel.Net.Nat">{{ natMap[scope.row.hook_tunnel.Net.Nat]  }}</span>
                                </template>
                                <template v-else>
                                    <img title="?" class="system" src="/system.svg" />
                                </template>
                            </div> 
                            <div class="flex">
                                <a href="javascript:;" class="a-line" 
                                :class="{yellow:scope.row.hook_tunnel.NeedReboot}" 
                                :title="$t('home.holeText')"
                                @click="handleTunnel(scope.row.hook_tunnel,scope.row,values)">
                                    <span>{{$t('home.jump')}}:{{scope.row.hook_tunnel.RouteLevel}}+{{scope.row.hook_tunnel.RouteLevelPlus}}</span>
                                </a>
                            </div>
                        </div>
                    </template>
                    <template v-else-if="scope.row &&!scope.row.hook_tunnel_load">
                        <div class="skeleton-animation">
                            <el-skeleton animated >
                                <template #template>
                                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                                    <el-skeleton-item variant="text" class="el-skeleton-item" />
                                    <el-skeleton-item variant="text" class="el-skeleton-item-last"/>
                                    <el-skeleton-item variant="text" class="el-skeleton-item2" />
                                </template>
                            </el-skeleton>
                        </div>
                    </template>
                    <div class="device-remark"></div>
                </template>

            </el-table-column>
        </template>
    </AccessBoolean>
</template>
<script>
import { useTunnel } from './tunnel';
import { injectGlobalData } from '@/provide';
import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import {useI18n} from 'vue-i18n';

export default {
    emits: ['edit','refresh'],
    setup() {

        const t = useI18n();
        const globalData = injectGlobalData();
        const machineId = computed(() => globalData.value.config.Client.Id);

        const tunnel = useTunnel();

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

        const handleTunnel = (_tunnel,row,access) => {
            if(machineId.value === _tunnel.MachineId){
                if(!access.TunnelChangeSelf){
                    ElMessage.success(t('common.access'));
                    return;
                }
            }else{
                if(!access.TunnelChangeOther){
                    ElMessage.success(t('common.access'));
                    return;
                }
            }
            _tunnel.device = row;
            tunnel.value.current = _tunnel;
            tunnel.value.showEdit = true;
        }
        return {
            handleTunnel,netImg,natMap
        }
    }
}
</script>
<style lang="stylus" scoped>

.el-switch.is-disabled{opacity :1;}

.green{font-weight:bold;}

.el-skeleton-item{width: 30%;margin-right:3%}
.el-skeleton-item-last{width: 30%;}
.el-skeleton-item2{width: 70%}


img.system,span.nat{
    height:1.4rem;
    margin-right:.4rem
    border: 1px solid #eee;
    line-height:1.4rem;
    vertical-align:middle;
}
html.dark img.system,html.dark span.nat{border-color:#575c61;}

span.nat{display:inline-block;padding:0 .2rem;margin-right:0;font-family: cursive;}
</style>