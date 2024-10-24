<template>
    <div class="home-list-wrap absolute" >
        <el-table border style="width: 100%" height="32px" size="small" @sort-change="handleSortChange" class="table-sort">
            <el-table-column prop="MachineId" label="设备名" width="110" sortable="custom" ></el-table-column>
            <el-table-column prop="Version" label="版本" width="110" sortable="custom"></el-table-column>
            <el-table-column prop="tunnel" label="网关" width="90" sortable="custom"></el-table-column>
            <el-table-column prop="tuntap" label="网卡IP" width="160" sortable="custom"></el-table-column>
            <el-table-column prop="forward" label=""></el-table-column>
            <el-table-column label="" width="74" fixed="right"></el-table-column>
        </el-table>
        <el-table :data="devices.page.List" stripe border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device  @edit="handleDeviceEdit" @refresh="handlePageRefresh"></Device>
            <Tunnel  @edit="handleTunnelEdit" @refresh="handleTunnelRefresh" @connections="handleTunnelConnections"></Tunnel>
            <Tuntap  @edit="handleTuntapEdit" @refresh="handleTuntapRefresh"></Tuntap>
            <Socks5 @edit="_handleForwardEdit" @sedit="handleSForwardEdit"></Socks5> 
            <Forward @edit="_handleForwardEdit" @sedit="handleSForwardEdit"></Forward> 
            <Oper  @refresh="handlePageRefresh" @access="handleAccessEdit"></Oper>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,sizes,prev,pager, next" :total="devices.page.Count"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
        <DeviceEdit v-if="devices.showDeviceEdit" v-model="devices.showDeviceEdit"  @change="handlePageChange" :data="devices.deviceInfo"></DeviceEdit>
        <AccessEdit v-if="devices.showAccessEdit" v-model="devices.showAccessEdit"  @change="handlePageChange" :data="devices.deviceInfo"></AccessEdit>
        <TunnelEdit v-if="tunnel.showEdit" v-model="tunnel.showEdit"  @change="handleTunnelRefresh"></TunnelEdit>
        <ConnectionsEdit v-if="connections.showEdit" v-model="connections.showEdit" ></ConnectionsEdit>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="handleTuntapRefresh"></TuntapEdit>
        <TuntapLease v-if="tuntap.showLease" v-model="tuntap.showLease"  @change="handleTuntapRefresh"></TuntapLease>
        <ForwardEdit v-if="forward.showEdit" v-model="forward.showEdit" ></ForwardEdit>
        <ForwardCopy v-if="forward.showCopy" v-model="forward.showCopy" ></ForwardCopy>
        <SForwardEdit v-if="sforward.showEdit" v-model="sforward.showEdit" ></SForwardEdit>
        <SForwardCopy v-if="sforward.showCopy" v-model="sforward.showCopy" ></SForwardCopy>
    </div>
</template>
<script>
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted, computed } from 'vue'
import Oper from './Oper.vue'
import Device from './Device.vue'
import DeviceEdit from './DeviceEdit.vue'
import AccessEdit from './AccessEdit.vue'
import Tuntap from './Tuntap.vue'
import TuntapEdit from './TuntapEdit.vue'
import TuntapLease from './TuntapLease.vue'
import Tunnel from './Tunnel.vue'
import TunnelEdit from './TunnelEdit.vue'
import Socks5 from './Socks5.vue'
import Forward from './Forward.vue'
import ForwardEdit from './ForwardEdit.vue'
import ForwardCopy from './ForwardCopy.vue'
import SForwardEdit from './SForwardEdit.vue'
import SForwardCopy from './SForwardCopy.vue'
import ConnectionsEdit from './ConnectionsEdit.vue'
import { ElMessage } from 'element-plus'
import { provideTuntap } from './tuntap'
import { provideTunnel } from './tunnel'
import { provideForward } from './forward'
import { provideConnections } from './connections'
import { provideSforward } from './sforward'
import { provideDevices } from './devices'
import { provideUpdater } from './updater'
import { provideAccess } from './access'
export default {
    components: {Oper,Device,DeviceEdit,AccessEdit,Tunnel,TunnelEdit,ConnectionsEdit, Tuntap,TuntapEdit,TuntapLease, Socks5, Forward,ForwardEdit,ForwardCopy,SForwardEdit,SForwardCopy },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            height: computed(()=>globalData.value.height-90),
        });

        const {devices, machineId, _getSignList, _getSignList1,
            handleDeviceEdit,handleAccessEdit, handlePageChange, handlePageSizeChange, handleDel,clearDevicesTimeout,setSort} = provideDevices();

        const {tuntap,_getTuntapInfo,handleTuntapEdit,handleTuntapRefresh,clearTuntapTimeout,getTuntapMachines,sortTuntapIP}  = provideTuntap();
        const {tunnel,_getTunnelInfo,handleTunnelEdit,handleTunnelRefresh,clearTunnelTimeout,sortTunnel} = provideTunnel();
        const {forward,_getForwardInfo,handleForwardEdit,_testTargetForwardInfo,clearForwardTimeout,getForwardMachines} = provideForward();
        const {sforward,_getSForwardInfo,handleSForwardEdit,_testLocalSForwardInfo,clearSForwardTimeout,getSForwardMachines} = provideSforward();
        const {connections,
            forwardConnections,_getForwardConnections,
            tuntapConnections,_getTuntapConnections,
            handleTunnelConnections,clearConnectionsTimeout
        } = provideConnections();

        const {_getUpdater,_subscribeUpdater,clearUpdaterTimeout} = provideUpdater();

        const {_getAccessInfo,clearAccessTimeout} = provideAccess();

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
            }
            fn.then(()=>{
                handlePageChange();
            }).catch(()=>{});
            
        }

        const _handleForwardEdit = (machineId) => {
            handleForwardEdit(machineId,devices.page.List.filter(c => c.MachineId == machineId)[0].MachineName);
        }

        const handlePageRefresh = (name)=>{
            devices.page.Request.Name = name || '';
            if(devices.page.Request.Name){
                //从虚拟网卡里查找
                devices.page.Request.Ids = getTuntapMachines(devices.page.Request.Name)
                //从端口转发里查找
                .concat(getForwardMachines(devices.page.Request.Name))
                //从服务器代理穿透里查找
                .concat(getSForwardMachines(devices.page.Request.Name))
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
            ElMessage.success({message:'刷新成功',grouping:true});  
        }
        const handlePageSearch = ()=>{
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            ElMessage.success({message:'刷新成功',grouping:true});  
        }

        onMounted(() => {
            handlePageChange();
            handleTunnelRefresh();
            handleTuntapRefresh();
            _getSignList();
            _getSignList1();
            _getTuntapInfo();
            _getTunnelInfo();
            _getForwardConnections();
            _getTuntapConnections();
            _getForwardInfo();
            _getSForwardInfo();

            _getUpdater();
            _subscribeUpdater();

            _getAccessInfo();

            _testTargetForwardInfo();
            _testLocalSForwardInfo();
        });
        onUnmounted(() => {
            clearDevicesTimeout();
            clearConnectionsTimeout();
            clearTuntapTimeout();
            clearTunnelTimeout();
            clearForwardTimeout();
            clearSForwardTimeout();

            clearUpdaterTimeout();

            clearAccessTimeout();
        });

        return {
            state,devices, machineId,handleSortChange,
            handleDeviceEdit,handleAccessEdit,handlePageRefresh,handlePageSearch, handlePageChange,handlePageSizeChange, handleDel,
            tuntap, handleTuntapEdit, handleTuntapRefresh,
            tunnel,connections, handleTunnelEdit, handleTunnelRefresh,handleTunnelConnections,
            forward,_handleForwardEdit,
            sforward,handleSForwardEdit
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
.table-sort 
{
    th{border-bottom:0}
}
.home-list-wrap{
    padding:1rem;

    .page{padding-top:1rem}
    .page-wrap{
        display:inline-block;
    }
}
</style>