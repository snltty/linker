<template>
    <div class="home-list-wrap absolute" >
        <Sort @sort="handleSortChange"></Sort>
        <el-table :data="devices.page.List" stripe border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device  @refresh="handlePageRefresh"></Device>
            <Tunnel  @refresh="handleTunnelRefresh"></Tunnel>
            <Tuntap></Tuntap>
            <Socks5  @refresh="handleSocks5Refresh"></Socks5> 
            <Forward ></Forward> 
            <Oper  @refresh="handlePageRefresh"></Oper>
        </el-table>
        <div class="page" :class="{'t-c':globalData.isPc}">
            <div class="page-wrap">
                <el-pagination small background :total="devices.page.Count"
                    :pager-count="globalData.isPc?7:3"
                    :layout="globalData.isPc?'total,sizes,prev,pager, next':'prev, pager, next'"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" 
                    :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
        <DeviceEdit v-if="devices.showDeviceEdit" v-model="devices.showDeviceEdit"  @change="handlePageChange" :data="devices.deviceInfo"></DeviceEdit>
        <AccessEdit v-if="devices.showAccessEdit" v-model="devices.showAccessEdit"  @change="handlePageChange" :data="devices.deviceInfo"></AccessEdit>
        <TunnelEdit v-if="tunnel.showEdit" v-model="tunnel.showEdit"  @change="handleTunnelRefresh"></TunnelEdit>
        <ConnectionsEdit v-if="connections.showEdit" v-model="connections.showEdit" ></ConnectionsEdit>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="handleTuntapRefresh"></TuntapEdit>
        <TuntapLease v-if="tuntap.showLease" v-model="tuntap.showLease"  @change="handleTuntapRefresh"></TuntapLease>
        <Socks5Edit v-if="socks5.showEdit" v-model="socks5.showEdit"  @change="handleSocks5Refresh"></Socks5Edit>
        <ForwardEdit v-if="forward.showEdit" v-model="forward.showEdit" ></ForwardEdit>
        <SForwardEdit v-if="sforward.showEdit" v-model="sforward.showEdit" ></SForwardEdit>
        <UpdaterConfirm v-if="updater.show" v-model="updater.show" ></UpdaterConfirm>
        
        <OperFirewall v-if="firewall.show" v-model="firewall.show" ></OperFirewall>
        <OperWakeup v-if="wakeup.show" v-model="wakeup.show" ></OperWakeup>
        <OperTransport v-if="transport.show" v-model="transport.show" ></OperTransport>
        <OperAction v-if="action.show" v-model="action.show" ></OperAction>
        <Stopwatch v-if="flow.showStopwatch" v-model="flow.showStopwatch" ></Stopwatch>
        <OperFlow v-if="flow.show" v-model="flow.show" ></OperFlow>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted, computed } from 'vue'
import { ElMessage } from 'element-plus'

import Sort from './Sort.vue'


import Device from '../../../components/device/Device.vue'
import DeviceEdit from '../../../components/device/DeviceEdit.vue'
import { provideDevices } from '../../../components/device/devices'

import AccessEdit from '../../../components/accesss/AccessEdit.vue'
import { provideAccess } from '../../../components/accesss/access'

import Tuntap from '../../../components/tuntap/Tuntap.vue'
import TuntapEdit from '../../../components/tuntap/TuntapEdit.vue'
import TuntapLease from '../../../components/tuntap/TuntapLease.vue'
import { provideTuntap } from '../../../components/tuntap/tuntap'


import Socks5 from '../../../components/socks5/Socks5.vue'
import Socks5Edit from '../../../components/socks5/Socks5Edit.vue'
import { provideSocks5 } from '../../../components/socks5/socks5'

import Tunnel from '../../../components/tunnel/Tunnel.vue'
import TunnelEdit from '../../../components/tunnel/TunnelEdit.vue'
import { provideTunnel } from '../../../components/tunnel/tunnel'

import Forward from '../../../components/forward/Forward.vue'
import ForwardEdit from '../../../components/forward/ForwardEdit.vue'
import { provideForward } from '../../../components/forward/forward'
import SForwardEdit from '../../../components/forward/SForwardEdit.vue'
import { provideSforward } from '../../../components/forward/sforward'

import ConnectionsEdit from '../../../components/connection/ConnectionsEdit.vue'
import { provideConnections } from '../../../components/connection/connections'

import { provideUpdater } from '../../../components/updater/updater'
import UpdaterConfirm from '../../../components/updater/UpdaterConfirm.vue'

import { provideFlow } from '../../../components/flow/flow'
import Stopwatch from '../../../components/flow/stopwatch/Index.vue'
import OperFlow from '../../../components/flow/OperDialog.vue'

import Oper from '../../../components/oper/Oper.vue'
import { provideOper } from '../../../components/oper/oper'
import OperFirewall from '../../../components/firewall/OperDialog.vue'
import OperWakeup from '../../../components/wakeup/OperDialog.vue'
import OperTransport from '../../../components/transport/OperDialog.vue'
import OperAction from '../../../components/action/OperDialog.vue'

import { provideDecenter } from '@/views/components/decenter/decenter'
import { provideFirewall } from '@/views/components/firewall/firewall'
import { provideWakeup } from '@/views/components/wakeup/wakeup'
import { provideTransport } from '@/views/components/transport/transport'
import { provideAction } from '@/views/components/action/action'


export default {
    components: {Sort,
        Device,DeviceEdit,
        AccessEdit,
        Tunnel,TunnelEdit,
        ConnectionsEdit,
        Tuntap,TuntapEdit,TuntapLease,
        Socks5, Socks5Edit,
        Forward,ForwardEdit,
        SForwardEdit ,UpdaterConfirm,
        Stopwatch,
        Oper,OperFirewall,OperWakeup ,OperTransport,OperAction,OperFlow
    },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            height: computed(()=>globalData.value.height-90)
        });

        const {devices, _getSignList, _getSignList1, handlePageChange, handlePageSizeChange,clearDevicesTimeout,setSort} = provideDevices();
        const {tuntap,_getTuntapInfo,handleTuntapRefresh,clearTuntapTimeout,getTuntapMachines,sortTuntapIP}  = provideTuntap();
        const {socks5,_getSocks5Info,handleSocks5Refresh,clearSocks5Timeout,getSocks5Machines,sortSocks5}  = provideSocks5();
        const {tunnel,_getTunnelInfo,getTunnelOperating,getRelayOperating,handleTunnelRefresh,clearTunnelTimeout,sortTunnel} = provideTunnel();
        const {forward} = provideForward();
        const {sforward} = provideSforward();
        const {connections,_getForwardConnections,_getTuntapConnections,_getSocks5Connections, clearConnectionsTimeout } = provideConnections();
        const {updater,_getUpdater,_subscribeUpdater,clearUpdaterTimeout} = provideUpdater();
        const {flow} = provideFlow();
        const {_getAccessInfo,clearAccessTimeout,handleAccesssRefresh} = provideAccess();
        const {oper} = provideOper();
        const {decenter,_getDecenterCounterInfo,handleDecenterRefresh,clearDecenterCounterTimeout} = provideDecenter();
        const {firewall} = provideFirewall();
        const {wakeup} = provideWakeup();
        const {transport} = provideTransport();
        const {action}  = provideAction();

        const handleSortChange = (row)=>{

            devices.page.Request.Prop = row.prop;
            devices.page.Request.Asc = row.order == 'ascending';
            
            let fn = new Promise((resolve,reject)=>{
                resolve();
            });
            if(row.prop == 'tunnel'){   
                const ids = sortTunnel(devices.page.Request.Asc);
                if(ids .length > 0){
                    fn = setSort(ids);
                }
            }else if(row.prop == 'tuntap'){
                const ids = sortTuntapIP(devices.page.Request.Asc);
                if(ids .length > 0){
                    fn = setSort(ids);
                }
            }else if(row.prop == 'socks5'){
                const ids = sortSocks5(devices.page.Request.Asc);
                if(ids .length > 0){
                    fn = setSort(ids);
                }
            }
            fn.then(()=>{
                handlePageChange();
            }).catch(()=>{});
            
        }

        const handlePageRefresh = (name)=>{
            devices.page.Request.Name = name || '';
            if(devices.page.Request.Name){
                //从虚拟网卡里查找
                devices.page.Request.Ids = getTuntapMachines(devices.page.Request.Name)
                .concat(getSocks5Machines(devices.page.Request.Name))
                .reduce((arr,id)=>{
                    if(arr.indexOf(id) == -1){
                        arr.push(id);
                    }
                    return arr;
               },[]);
            }else{
                devices.page.Request.Ids = [];
            }
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            handleSocks5Refresh();
            handleForwardRefresh();
            handleSForwardRefresh();
            handleAccesssRefresh();
            ElMessage.success({message:'刷新成功',grouping:true});  
        }
        const handlePageSearch = ()=>{
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            handleSocks5Refresh();
            handleAccesssRefresh();
            handleDecenterRefresh();
            ElMessage.success({message:'刷新成功',grouping:true});  
        }

        onMounted(() => {
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            handleSocks5Refresh();
            handleAccesssRefresh();
            handleDecenterRefresh();
            
            _getSignList();
            _getSignList1();
            _getTuntapInfo();
            _getSocks5Info();
            _getTunnelInfo();
            getTunnelOperating();
            getRelayOperating();
            _getForwardConnections();
            _getTuntapConnections();
            _getSocks5Connections();
            _getUpdater();
            _subscribeUpdater();

            _getAccessInfo();

            _getDecenterCounterInfo();
        });
        onUnmounted(() => {
            clearDevicesTimeout();
            clearConnectionsTimeout();
            clearTuntapTimeout();
            clearSocks5Timeout();
            clearTunnelTimeout();

            clearUpdaterTimeout();

            clearAccessTimeout();

            clearDecenterCounterTimeout();
        });

        return {
            state,globalData,devices,handleSortChange,
            handlePageRefresh,handlePageSearch, handlePageChange,handlePageSizeChange,
            tuntap,
            socks5, handleSocks5Refresh,
            tunnel,connections, handleTunnelRefresh,
            forward,
            sforward,
            updater,flow,oper,firewall,wakeup,transport,action
        }
    }
}
</script>
<style lang="stylus">
.table-sort.el-table
{
    th.el-table__cell.is-leaf{border-bottom:0}
    .el-table__inner-wrapper:before{height:0}
}
</style>
<style lang="stylus" scoped>
.home-list-wrap{
    padding:1rem;

    .page{padding-top:1rem}
    .page-wrap{
        display:inline-block;
    }
}
</style>