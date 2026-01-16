<template>
    <div class="home-list-wrap absolute flex flex-column flex-nowrap" >
        <Sort @sort="handleSortChange"></Sort>
        <el-table :data="devices.page.List" stripe border style="width: 100%" size="small" class="flex-1">
            <Device  @refresh="handlePageRefresh"></Device>
            <Tunnel  @refresh="deviceRefreshHook('tunnel')"></Tunnel>
            <Tuntap></Tuntap>
            <Socks5  @refresh="deviceRefreshHook('socks5')"></Socks5> 
            <Forward ></Forward> 
            <Oper  @refresh="handlePageRefresh"></Oper>
        </el-table>
        <div class="page" :class="{'t-c':state.center}">
            <div class="page-wrap">
                <el-pagination small background :total="devices.page.Count"
                    :pager-count="state.paperCount"
                    :layout="state.paperLayout"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" 
                    :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
        <DeviceEdit v-if="devices.showDeviceEdit" v-model="devices.showDeviceEdit"  @change="handlePageChange" :data="devices.deviceInfo"></DeviceEdit>
        <AccessEdit v-if="devices.showAccessEdit" v-model="devices.showAccessEdit"  @change="handlePageChange" :data="devices.deviceInfo"></AccessEdit>
        <TunnelEdit v-if="tunnel.showEdit" v-model="tunnel.showEdit"  @change="deviceRefreshHook('tunnel')"></TunnelEdit>
        <ConnectionsEdit v-if="connections.showEdit" v-model="connections.showEdit" ></ConnectionsEdit>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="deviceRefreshHook('tuntap')"></TuntapEdit>
        <TuntapLease v-if="tuntap.showLease" v-model="tuntap.showLease"  @change="deviceRefreshHook('tuntap')"></TuntapLease>
        <Socks5Edit v-if="socks5.showEdit" v-model="socks5.showEdit"  @change="deviceRefreshHook('socks5')"></Socks5Edit>
        <ForwardEdit v-if="forward.showEdit" v-model="forward.showEdit" ></ForwardEdit>
        <SForwardEdit v-if="sforward.showEdit" v-model="sforward.showEdit" ></SForwardEdit>
        <UpdaterConfirm v-if="updater.show" v-model="updater.show" ></UpdaterConfirm>
        
        <OperFirewall v-if="firewall.show" v-model="firewall.show" ></OperFirewall>
        <OperWakeup v-if="wakeup.show" v-model="wakeup.show" ></OperWakeup>
        <OperTransport v-if="transport.show" v-model="transport.show" ></OperTransport>
        <OperAction v-if="action.show" v-model="action.show" ></OperAction>
        <Stopwatch v-if="flow.showStopwatch" v-model="flow.showStopwatch" ></Stopwatch>
        <OperFlow v-if="flow.show" v-model="flow.show" ></OperFlow>
        <OperWlist v-if="wlist.show" v-model="wlist.show" @change="deviceRefreshHook('wlist')" ></OperWlist>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted, computed } from 'vue'

import Sort from './Sort.vue'

import Device from '../../../components/device/Device.vue'
import DeviceEdit from '../../../components/device/DeviceEdit.vue'
import { provideDevices } from '../../../components/device/devices'

import AccessEdit from '../../../components/accesss/AccessEdit.vue'
import { provideAccess } from '../../../components/accesss/access'

import Socks5 from '../../../components/socks5/Socks5.vue'
import Socks5Edit from '../../../components/socks5/Socks5Edit.vue'
import { provideSocks5 } from '../../../components/socks5/socks5'

import Tunnel from '../../../components/tunnel/Tunnel.vue'
import TunnelEdit from '../../../components/tunnel/TunnelEdit.vue'
import { provideTunnel } from '../../../components/tunnel/tunnel'

import { provideUpdater } from '../../../components/updater/updater'
import UpdaterConfirm from '../../../components/updater/UpdaterConfirm.vue'


import Tuntap from '../../../components/tuntap/Tuntap.vue'
import TuntapEdit from '../../../components/tuntap/TuntapEdit.vue'
import TuntapLease from '../../../components/tuntap/TuntapLease.vue'
import { provideTuntap } from '../../../components/tuntap/tuntap'

import ConnectionsEdit from '../../../components/tunnel/ConnectionsEdit.vue'
import { provideConnections } from '../../../components/tunnel/connections'


import Forward from '../../../components/forward/Forward.vue'
import ForwardEdit from '../../../components/forward/ForwardEdit.vue'
import { provideForward } from '../../../components/forward/forward'
import SForwardEdit from '../../../components/forward/SForwardEdit.vue'
import { provideSforward } from '../../../components/forward/sforward'

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
import { provideWlist } from '@/views/components/wlist/wlist'
import OperWlist from '../../../components/wlist/OperDialog.vue'


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
        Oper,OperFirewall,OperWakeup ,OperTransport,OperAction,OperFlow,OperWlist
    },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            center: computed(()=>globalData.value.isPc),
            paperCount: computed(()=>globalData.value.isPc?7:3),
            paperLayout: computed(()=>globalData.value.isPc?'total,sizes,prev,pager, next':'prev, pager, next'),
        });

        const {devices,deviceAddHook,deviceRefreshHook, deviceStartProcess, handlePageChange, handlePageSizeChange,deviceClearTimeout,setSort} = provideDevices();
        const {forward} = provideForward();
        const {sforward} = provideSforward();
        const {flow} = provideFlow();
        const {oper} = provideOper();
        const {firewall} = provideFirewall();
        const {wakeup} = provideWakeup();
        const {transport} = provideTransport();
        const {action}  = provideAction();

        const {accessDataFn,accessProcessFn,accessRefreshFn} = provideAccess();
        deviceAddHook('access',accessDataFn,accessProcessFn,accessRefreshFn);
        const {decenter,counterDataFn,counterProcessFn,counterRefreshFn} = provideDecenter();
        deviceAddHook('counter',counterDataFn,counterProcessFn,counterRefreshFn);
        const {socks5,socks5DataFn,socks5ProcessFn,socks5RefreshFn,getSocks5Machines,sortSocks5}  = provideSocks5();
        deviceAddHook('socks5',socks5DataFn,socks5ProcessFn,socks5RefreshFn);
        const {tunnel,tunnelDataFn,tunnelProcessFn,tunnelRefreshFn,sortTunnel} = provideTunnel();
        deviceAddHook('tunnel',tunnelDataFn,tunnelProcessFn,tunnelRefreshFn);
        const {updater, updaterDataFn, updaterProcessFn,updaterRefreshFn,updaterSubscribe, updaterClearTimeout} = provideUpdater();
        deviceAddHook('updater',updaterDataFn,updaterProcessFn,updaterRefreshFn);
        const {tuntap,tuntapDataFn,tuntapProcessFn,tuntapRefreshFn,getTuntapMachines,sortTuntapIP}  = provideTuntap();
        deviceAddHook('tuntap',tuntapDataFn,tuntapProcessFn,tuntapRefreshFn);
        const {connections,connectionDataFn,connectionProcessFn,connectionRefreshFn } = provideConnections();
        deviceAddHook('connection',connectionDataFn,connectionProcessFn,connectionRefreshFn);
        const {wlist,wlistDataFn,wlistProcessFn,wlistRefreshFn} = provideWlist();
        deviceAddHook('wlist',wlistDataFn,wlistProcessFn,wlistRefreshFn);
        
        
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
        }

        onMounted(() => {
            window.dispatchEvent(new Event('resize'));
            handlePageChange();
            
            deviceStartProcess();
            updaterSubscribe();
        });
        onUnmounted(() => {
            deviceClearTimeout();
            updaterClearTimeout();
        });

        return {
            state,devices,deviceRefreshHook,handleSortChange, handlePageRefresh, handlePageChange,handlePageSizeChange,
            tuntap,
            socks5,
            tunnel,connections,
            forward,
            sforward,
            updater,flow,oper,firewall,wakeup,transport,action,wlist
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