<template>
    <div class="home-list-wrap absolute" >
        <Sort @sort="handleSortChange"></Sort>
        <el-table :data="devices.page.List" stripe border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device  @edit="handleDeviceEdit" @refresh="handlePageRefresh"></Device>
            <Tunnel  @edit="handleTunnelEdit" @refresh="handleTunnelRefresh" @connections="handleTunnelConnections"></Tunnel>
            <Tuntap  @edit="handleTuntapEdit" @refresh="handleTuntapRefresh"></Tuntap>
            <Socks5  @edit="handleSocks5Edit" @refresh="handleSocks5Refresh"></Socks5> 
            <Forward  @edit="handleForwardEdit" @sedit="handleSForwardEdit"></Forward> 
            <Oper  @refresh="handlePageRefresh" @access="handleAccessEdit"></Oper>
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
        <Stopwatch v-if="flow.show" v-model="flow.show" ></Stopwatch>
        <OperRoutes v-if="oper.showRoutes" v-model="oper.showRoutes" ></OperRoutes>
        <OperFirewall v-if="oper.showFirewall" v-model="oper.showFirewall" ></OperFirewall>
        <OperWakeup v-if="oper.showWakeup" v-model="oper.showWakeup" ></OperWakeup>
        <OperTransport v-if="oper.showTransport" v-model="oper.showTransport" ></OperTransport>
        <OperAction v-if="oper.showAction" v-model="oper.showAction" ></OperAction>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted, computed } from 'vue'
import { ElMessage } from 'element-plus'

import Sort from './Sort.vue'


import Device from './Device.vue'
import DeviceEdit from './DeviceEdit.vue'
import { provideDevices } from './devices'

import AccessEdit from './AccessEdit.vue'
import { provideAccess } from './access'

import Tuntap from './Tuntap.vue'
import TuntapEdit from './TuntapEdit.vue'
import TuntapLease from './TuntapLease.vue'
import { provideTuntap } from './tuntap'


import Socks5 from './Socks5.vue'
import Socks5Edit from './Socks5Edit.vue'
import { provideSocks5 } from './socks5'

import Tunnel from './Tunnel.vue'
import TunnelEdit from './TunnelEdit.vue'
import { provideTunnel } from './tunnel'

import Forward from './Forward.vue'
import ForwardEdit from './ForwardEdit.vue'
import { provideForward } from './forward'

import SForwardEdit from './SForwardEdit.vue'
import { provideSforward } from './sforward'

import ConnectionsEdit from './ConnectionsEdit.vue'
import { provideConnections } from './connections'

import { provideUpdater, useUpdater } from './updater'
import UpdaterConfirm from './UpdaterConfirm.vue'

import { provideFlow } from './flow'
import Stopwatch from './Stopwatch.vue'

import Oper from './Oper.vue'
import { provideOper } from './oper'
import OperRoutes from './OperRoutes.vue'
import OperFirewall from './OperFirewall.vue'
import OperWakeup from './OperWakeup.vue'
import OperTransport from './OperTransport.vue'
import OperAction from './OperAction.vue'


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
        Oper,OperRoutes,OperFirewall,OperWakeup ,OperTransport,OperAction
    },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            height: computed(()=>globalData.value.height-90)
        });

        const {devices, machineId, _getSignList, _getSignList1,
            handleDeviceEdit,handleAccessEdit, handlePageChange, handlePageSizeChange, handleDel,clearDevicesTimeout,setSort} = provideDevices();

        const {tuntap,_getTuntapInfo,handleTuntapEdit,handleTuntapRefresh,clearTuntapTimeout,getTuntapMachines,sortTuntapIP}  = provideTuntap();
        const {socks5,_getSocks5Info,handleSocks5Edit,handleSocks5Refresh,clearSocks5Timeout,getSocks5Machines,sortSocks5}  = provideSocks5();
        const {tunnel,_getTunnelInfo,getTunnelOperating,getRelayOperating,handleTunnelEdit,handleTunnelRefresh,clearTunnelTimeout,sortTunnel} = provideTunnel();
        const {forward,_getForwardCountInfo,handleForwardEdit,clearForwardTimeout,handleForwardRefresh} = provideForward();
        const {sforward,_getSForwardCountInfo,handleSForwardEdit,clearSForwardTimeout,handleSForwardRefresh} = provideSforward();
        const {connections,
            forwardConnections,_getForwardConnections,
            tuntapConnections,_getTuntapConnections,
            socks5Connections,_getSocks5Connections,
            handleTunnelConnections,clearConnectionsTimeout
        } = provideConnections();

        const {updater,_getUpdater,_subscribeUpdater,clearUpdaterTimeout} = provideUpdater();
        const {flow} = provideFlow();

        const {_getAccessInfo,clearAccessTimeout,handleAccesssRefresh} = provideAccess();

        const {oper} = provideOper();

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
            ElMessage.success({message:'刷新成功',grouping:true});  
        }

        onMounted(() => {
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            handleSocks5Refresh();
            handleForwardRefresh();
            handleSForwardRefresh();
            handleAccesssRefresh();
            
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
            _getForwardCountInfo();
            _getSForwardCountInfo();
            _getUpdater();
            _subscribeUpdater();

            _getAccessInfo();

            
        });
        onUnmounted(() => {
            clearDevicesTimeout();
            clearConnectionsTimeout();
            clearTuntapTimeout();
            clearSocks5Timeout();
            clearTunnelTimeout();
            clearForwardTimeout();
            clearSForwardTimeout();

            clearUpdaterTimeout();

            clearAccessTimeout();
        });

        return {
            state,globalData,devices, machineId,handleSortChange,
            handleDeviceEdit,handleAccessEdit,handlePageRefresh,handlePageSearch, handlePageChange,handlePageSizeChange, handleDel,
            tuntap, handleTuntapEdit, handleTuntapRefresh,
            socks5, handleSocks5Edit, handleSocks5Refresh,
            tunnel,connections, handleTunnelEdit, handleTunnelRefresh,handleTunnelConnections,
            forward,handleForwardEdit,
            sforward,handleSForwardEdit,
            updater,flow,oper
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