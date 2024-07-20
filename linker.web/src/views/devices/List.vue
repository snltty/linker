<template>
    <div class="home-list-wrap absolute" >
        <el-table :data="devices.page.List" stripe border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device  @edit="handleDeviceEdit" @refresh="handlePageRefresh"></Device>
            <Tunnel  @edit="handleTunnelEdit" @refresh="handleTunnelRefresh" @connections="handleTunnelConnections"></Tunnel>
            <Tuntap  @edit="handleTuntapEdit" @refresh="handleTuntapRefresh"></Tuntap>
            <Forward @edit="_handleForwardEdit" @sedit="handleSForwardEdit"></Forward> 
            <Oper  @refresh="handlePageRefresh"></Oper>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,sizes,prev,pager, next" :total="devices.page.Count"
                    :page-size="devices.page.Request.Size" :current-page="devices.page.Request.Page"
                    @current-change="handlePageChange" @size-change="handlePageSizeChange" :page-sizes="[10, 20, 50, 100,255]" />
            </div>
        </div>
        <DeviceEdit v-if="devices.showDeviceEdit" v-model="devices.showDeviceEdit"  @change="handlePageChange" :data="devices.deviceInfo"></DeviceEdit>
        <TunnelEdit v-if="tunnel.showEdit" v-model="tunnel.showEdit"  @change="handleTunnelRefresh"></TunnelEdit>
        <ConnectionsEdit v-if="connections.showEdit" v-model="connections.showEdit" ></ConnectionsEdit>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="handleTuntapRefresh"></TuntapEdit>
        <ForwardEdit v-if="forward.showEdit" v-model="forward.showEdit" ></ForwardEdit>
        <ForwardCopy v-if="forward.showCopy" v-model="forward.showCopy" ></ForwardCopy>
        <SForwardEdit v-if="sforward.showEdit" v-model="sforward.showEdit" ></SForwardEdit>
        <SForwardCopy v-if="sforward.showCopy" v-model="sforward.showCopy" ></SForwardCopy>
    </div>
</template>
<script>
import { subWebsocketState } from '@/apis/request.js'
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted,  onUnmounted, computed } from 'vue'
import Oper from './Oper.vue'
import Device from './Device.vue'
import DeviceEdit from './DeviceEdit.vue'
import Tuntap from './Tuntap.vue'
import TuntapEdit from './TuntapEdit.vue'
import Tunnel from './Tunnel.vue'
import TunnelEdit from './TunnelEdit.vue'
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
export default {
    components: {Oper,Device,DeviceEdit,Tunnel,TunnelEdit,ConnectionsEdit, Tuntap,TuntapEdit,  Forward,ForwardEdit,ForwardCopy,SForwardEdit,SForwardCopy },
    setup(props) {

        const globalData = injectGlobalData();
        const state = reactive({
            height: computed(()=>globalData.value.height-60),
        });

        const {devices, machineId, _getSignList, _getSignList1, 
            handleDeviceEdit, handlePageChange, handlePageSizeChange, handleDel,clearDevicesTimeout} = provideDevices();

        const {tuntap,_getTuntapInfo,handleTuntapEdit,handleTuntapRefresh,clearTuntapTimeout,getTuntapMachines}  = provideTuntap();
        const {tunnel,_getTunnelInfo,handleTunnelEdit,handleTunnelRefresh,clearTunnelTimeout} = provideTunnel();
        const {forward,_getForwardInfo,handleForwardEdit,_testTargetForwardInfo,_testListenForwardInfo,clearForwardTimeout,getForwardMachines} = provideForward();
        const {sforward,_getSForwardInfo,handleSForwardEdit,_testLocalSForwardInfo,clearSForwardTimeout,getSForwardMachines} = provideSforward();
        const {connections,
            forwardConnections,_getForwardConnections,
            tuntapConnections,_getTuntapConnections,
            handleTunnelConnections,clearConnectionsTimeout
        } = provideConnections();

        const {_getUpdater,clearUpdaterTimeout} = provideUpdater();

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
            subWebsocketState((state) => { 
                if (state){
                    handlePageChange();
                    _getSignList();
                } 
                
            });
            
            _getSignList();
            _getSignList1();
            _getTuntapInfo();
            _getTunnelInfo();
            _getForwardConnections();
            _getTuntapConnections();
            _getForwardInfo();
            _getSForwardInfo();

            _getUpdater();

            _testTargetForwardInfo();
            _testListenForwardInfo();
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
        });

        return {
            state,devices, machineId,
            handleDeviceEdit,handlePageRefresh,handlePageSearch, handlePageChange,handlePageSizeChange, handleDel,
            tuntap, handleTuntapEdit, handleTuntapRefresh,
            tunnel,connections, handleTunnelEdit, handleTunnelRefresh,handleTunnelConnections,
            forward,_handleForwardEdit,
            sforward,handleSForwardEdit
        }
    }
}
</script>
<style lang="stylus" scoped>
.home-list-wrap{
    padding:1rem;

    .page{padding-top:1rem}
    .page-wrap{
        display:inline-block;
    }
}
</style>