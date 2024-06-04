<template>
    <div class="home-list-wrap absolute" >
        <el-table :data="state.page.List" border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device  @edit="handleDeviceEdit" @refresh="handlePageRefresh"></Device>
            <Tunnel  @edit="handleTunnelEdit" @refresh="handleTunnelRefresh" @connections="handleTunnelConnections"></Tunnel>
            <Tuntap  @edit="handleTuntapEdit" @refresh="handleTuntapRefresh"></Tuntap>
            <Forward @edit="handleForwardEdit" @refresh="handleForwardRefresh"></Forward>  
            <Info></Info>          
            <el-table-column label="操作" width="66" fixed="right">
                <template #default="scope">
                    <el-popconfirm v-if="scope.row.showDel" confirm-button-text="确认"
                        cancel-button-text="取消" title="删除不可逆，是否确认?" @confirm="handleDel(scope.row.MachineName)">
                        <template #reference>
                            <el-button type="danger" size="small"><el-icon><Delete /></el-icon></el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,prev, pager, next" :total="state.page.Count"
                    :page-size="state.page.Request.Size" :current-page="state.page.Request.Page"
                    @current-change="handlePageChange" />
            </div>
        </div>
        <DeviceEdit v-if="state.showDeviceEdit" v-model="state.showDeviceEdit"  @change="handlePageChange" :data="state.deviceInfo"></DeviceEdit>
        <TunnelEdit v-if="tunnel.showEdit" v-model="tunnel.showEdit"  @change="handleTunnelChange"></TunnelEdit>
        <ConnectionsEdit v-if="connections.showEdit" v-model="connections.showEdit" ></ConnectionsEdit>
        <TuntapEdit v-if="tuntap.showEdit" v-model="tuntap.showEdit"  @change="handleTuntapChange"></TuntapEdit>
        <ForwardEdit v-if="forward.showEdit" v-model="forward.showEdit"  @change="handleTuntapChange"></ForwardEdit>
    </div>
</template>
<script>
import { getSignInList, signInDel } from '@/apis/signin.js'
import { subWebsocketState } from '@/apis/request.js'
import { getTuntapInfo,refreshTuntap } from '@/apis/tuntap'
import { getForwardInfo ,refreshForward} from '@/apis/forward'
import { getTunnelInfo ,refreshTunnel} from '@/apis/tunnel'
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted, ref, nextTick, onUnmounted, computed, provide } from 'vue'
import Device from './Device.vue'
import DeviceEdit from './DeviceEdit.vue'
import Info from './Info.vue'
import Tuntap from './Tuntap.vue'
import TuntapEdit from './TuntapEdit.vue'
import Tunnel from './Tunnel.vue'
import TunnelEdit from './TunnelEdit.vue'
import Forward from './Forward.vue'
import ForwardEdit from './ForwardEdit.vue'
import ConnectionsEdit from './ConnectionsEdit.vue'
import { ElMessage } from 'element-plus'
import { getConnections } from '@/apis/connections'
export default {
    components: {Device,DeviceEdit,Info,Tunnel,TunnelEdit,ConnectionsEdit, Tuntap,TuntapEdit,  Forward,ForwardEdit },
    setup(props) {

        const globalData = injectGlobalData();
        const machineName = computed(() => globalData.value.config.Client.Name);
       
        const state = reactive({
            page: {
                Request: { Page: 1, Size: 10, GroupId: globalData.value.groupid },
                Count: 0,
                List: []
            },

            showDeviceEdit:false,
            deviceInfo: null,

            height: computed(()=>globalData.value.height-60),
        });

        const tuntap = ref({
            timer:0,
            showEdit:false,
            current: null,
            list: {},
            hashcode: 0,
        });
        provide('tuntap',tuntap);
        const _getTuntapInfo = () => {
            if (globalData.value.connected) {
                getTuntapInfo(tuntap.value.hashcode.toString()).then((res) => {
                    tuntap.value.hashcode = res.HashCode;
                    if (res.List) {
                        for (let j in res.List) {
                            res.List[j].running = res.List[j].Status == 2;
                            res.List[j].loading = res.List[j].Status == 1;
                        }
                        tuntap.value.list = res.List;
                    }
                    tuntap.value.timer = setTimeout(_getTuntapInfo, 200);
                }).catch(() => {
                    tuntap.value.timer = setTimeout(_getTuntapInfo, 200);
                });
            } else {
                tuntap.value.timer = setTimeout(_getTuntapInfo, 1000);
            }
        }
        const handleTuntapEdit = (_tuntap)=>{
            tuntap.value.current = _tuntap;
            tuntap.value.showEdit = true;

        }
        const handleTuntapChange = () => {
            _getSignList();
        }
        const handleTuntapRefresh = ()=>{
            refreshTuntap();
            ElMessage.success('刷新成功');
        }


        const tunnel = ref({
            timer:0,
            showEdit:false,
            current: null,
            list: {},
            hashcode: 0,
        });
        provide('tunnel',tunnel);
        const _getTunnelInfo = () => {
            if (globalData.value.connected) {
                getTunnelInfo(tunnel.value.hashcode.toString()).then((res) => {
                    tunnel.value.hashcode = res.HashCode;
                    if (res.List) {
                        tunnel.value.list = res.List;
                    }
                    tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
                }).catch(() => {
                    tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
                });
            } else {
                tunnel.value.timer = setTimeout(_getTunnelInfo, 1000);
            }
        }
        const handleTunnelEdit = (_tunnel)=>{
            tunnel.value.current = _tunnel;
            tunnel.value.showEdit = true;
        }
        const handleTunnelChange = () => {
            _getSignList();
        }
        const handleTunnelRefresh = ()=>{
            refreshTunnel();
            ElMessage.success('刷新成功');
        }

        const connections = ref({
            timer:0,
            showEdit:false,
            current: [],
            list: {},
            hashcode: 0,
        });
        provide('connections',connections);
        const _getConnections = ()=>{
            if (globalData.value.connected) {
                getConnections(connections.value.hashcode.toString()).then((res)=>{
                    connections.value.hashcode = res.HashCode;
                    if (res.List) {
                        for(let j in res.List){
                            res.List[j] = Object.values(res.List[j]);
                        }
                        connections.value.list = res.List;
                    }
                    connections.value.timer = setTimeout(_getConnections, 1000);
                }).catch((e)=>{
                    connections.value.timer = setTimeout(_getConnections, 1000);
                })
            }else {
                connections.value.timer = setTimeout(_getConnections, 1000);
            }
        }
        const handleTunnelConnections = (machineName)=>{
            connections.value.current = machineName;
            connections.value.showEdit = true;
        }

        const forward = ref({
            timer:0,
            showEdit:false,
            current: null,
            list: {}
        });
        provide('forward',forward);
        const _getForwardInfo = () => {
            if (globalData.value.connected) {
                getForwardInfo().then((res) => {
                    forward.value.list = res;

                    forward.value.timer = setTimeout(_getForwardInfo, 1000);
                }).catch(() => {
                    forward.value.timer = setTimeout(_getForwardInfo, 1000);
                });
            }else {
                forward.value.timer = setTimeout(_getForwardInfo, 1000);
            }
        }
        const handleForwardEdit = (machineName)=>{
            forward.value.current = machineName;
            forward.value.showEdit = true;
        }
        const handleForwardChange = () => {
            _getSignList();
        }
        const handleForwardRefresh = ()=>{
            _getSignList();
            ElMessage.success('刷新成功');
        }

        const _getSignList = () => {
            state.page.Request.GroupId = globalData.value.groupid;
            getSignInList(state.page.Request).then((res) => {
                state.page.Request = res.Request;
                state.page.Count = res.Count;
                for (let j in res.List) {
                    res.List[j].showTunnel = machineName.value != res.List[j].MachineName;
                    res.List[j].showForward = machineName.value != res.List[j].MachineName;
                    res.List[j].showDel = machineName.value != res.List[j].MachineName && res.List[j].Connected == false;
                    res.List[j].isSelf = machineName.value == res.List[j].MachineName;
                }
                state.page.List = res.List;
            }).catch((err) => { });
        }
        const _getSignList1 = () => {
            if (globalData.value.connected) {
                state.page.Request.GroupId = globalData.value.groupid;
                getSignInList(state.page.Request).then((res) => {
                    for (let j in res.List) {
                        const item = state.page.List.filter(c=>c.MachineName == res.List[j].MachineName)[0];
                        if(item){
                            item.Connected = res.List[j].Connected;
                            item.Version = res.List[j].Version;
                            item.LastSignIn = res.List[j].LastSignIn;
                            item.Args = res.List[j].Args;
                            item.showTunnel = machineName.value != res.List[j].MachineName;
                            item.showForward = machineName.value != res.List[j].MachineName;
                            item.showDel = machineName.value != res.List[j].MachineName && res.List[j].Connected == false;
                            item.isSelf = machineName.value == res.List[j].MachineName;
                        }
                    }
                    setTimeout(_getSignList1, 5000);
                }).catch((err) => { 
                    setTimeout(_getSignList1, 5000);
                });
            }else{
                setTimeout(_getSignList1, 5000);
            }
        }
        
        const handleDeviceEdit = (row)=>{
            state.deviceInfo = row;
            state.showDeviceEdit = true;
        }
        const handlePageRefresh = ()=>{
            handlePageChange();
            refreshTunnel();
            refreshTuntap();
            ElMessage.success('刷新成功');  
        }
        const handlePageChange = () => {
            _getSignList();
        }
        const handleDel = (name) => {
            signInDel(name).then(() => {
                _getSignList();
            });
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
            _getConnections();
            _getForwardInfo();
        });
        onUnmounted(() => {
            clearTimeout(tuntap.value.timer);
            clearTimeout(tunnel.value.timer);
            clearTimeout(connections.value.timer);
        });

        return {
            machineName, state, 
            handleDeviceEdit,handlePageRefresh, handlePageChange, handleDel,
            tuntap, handleTuntapEdit, handleTuntapChange, handleTuntapRefresh,
            tunnel,connections, handleTunnelEdit, handleTunnelChange, handleTunnelRefresh,handleTunnelConnections,
            forward, handleForwardEdit,handleForwardChange,handleForwardRefresh
        }
    }
}
</script>
<style lang="stylus" scoped>
.home-list-wrap{
    padding:1rem;

    .green{color:green;}

    .page{padding-top:1rem}
    .page-wrap{
        display:inline-block;
    }
}
</style>