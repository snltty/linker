<template>
    <div class="home-list-wrap absolute" ref="wrap">
        <el-table :data="state.page.List" border style="width: 100%" :height="`${state.height}px`" size="small">
            <Device @change="handlePageChange" @edit="handleDeviceEdit" @refresh="handlePageRefresh"></Device>
            <template v-if="state.tunnelInfos">
                <Tunnel @change="handleTunnelChange" @edit="handleTunnelEdit" @refresh="handleTunnelRefresh" :data="state.tunnelInfos"></Tunnel>
            </template>
            <template v-if="state.tuntapInfos">
                <Tuntap @change="handleTuntapChange" @edit="handleTuntapEdit" @refresh="handleTuntapRefresh" :data="state.tuntapInfos"></Tuntap>
            </template>
            <template v-if="state.forwardInfos">
                <Forward @change="handleForwardChange" @edit="handleForwardEdit" @refresh="handleForwardRefresh" :data="state.forwardInfos"></Forward>
            </template>    
            <Info></Info>   
           
            <el-table-column label="操作" width="66">
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
        <TunnelEdit v-if="state.showTunnelEdit" v-model="state.showTunnelEdit"  @change="handleTunnelChange" :data="state.tunnelInfo"></TunnelEdit>
        <TuntapEdit v-if="state.showTuntapEdit" v-model="state.showTuntapEdit"  @change="handleTuntapChange" :data="state.tuntapInfo"></TuntapEdit>
        <ForwardEdit v-if="state.showForwardEdit" v-model="state.showForwardEdit"  @change="handleTuntapChange" :data="state.forwardInfo"></ForwardEdit>
    </div>
</template>
<script>
import { getSignList, updateSignInDel } from '@/apis/signin.js'
import { subWebsocketState } from '@/apis/request.js'
import { getTuntapInfo,refreshTuntap } from '@/apis/tuntap'
import { getForwardInfo ,refreshForward} from '@/apis/forward'
import { getTunnelInfo ,refreshTunnel} from '@/apis/tunnel'
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted, ref, nextTick, onUnmounted, computed } from 'vue'
import Device from './Device.vue'
import DeviceEdit from './DeviceEdit.vue'
import Info from './Info.vue'
import Tuntap from './Tuntap.vue'
import TuntapEdit from './TuntapEdit.vue'
import Tunnel from './Tunnel.vue'
import TunnelEdit from './TunnelEdit.vue'
import Forward from './Forward.vue'
import ForwardEdit from './ForwardEdit.vue'
import { ElMessage } from 'element-plus'
export default {
    components: {Device,DeviceEdit,Info,Tunnel,TunnelEdit, Tuntap,TuntapEdit,  Forward,ForwardEdit },
    setup(props) {

        const globalData = injectGlobalData();
        const machineName = computed(() => globalData.value.config.Client.Name);
        const wrap = ref(null);
        const state = reactive({
            page: {
                Request: { Page: 1, Size: 10, GroupId: globalData.value.groupid },
                Count: 0,
                List: []
            },

            showDeviceEdit:false,
            deviceInfo: null,

            showTuntapEdit:false,
            tuntapInfo: null,
            tuntapInfos: null,
            tuntapHashCode: 0,

            showTunnelEdit:false,
            tunnelInfo: null,
            tunnelInfos: null,
            tunnelHashCode: 0,

            showForwardEdit:false,
            forwardInfos: null,
            forwardInfo : null,

            height: 0,
        });

        let tuntapTimer = 0;
        const _getTuntapInfo = () => {
            if (globalData.value.connected) {
                getTuntapInfo(state.tuntapHashCode.toString()).then((res) => {
                    state.tuntapHashCode = res.HashCode;
                    if (res.List) {
                        state.tuntapInfos = null;
                        nextTick(() => {
                            for (let j in res.List) {
                                res.List[j].running = res.List[j].Status == 2;
                                res.List[j].loading = res.List[j].Status == 1;
                            }
                            state.tuntapInfos = res.List;
                        });
                    }
                    tuntapTimer = setTimeout(_getTuntapInfo, 200);
                }).catch(() => {
                    tuntapTimer = setTimeout(_getTuntapInfo, 200);
                });
            } else {
                tuntapTimer = setTimeout(_getTuntapInfo, 1000);
            }
        }
        const handleTuntapEdit = (tuntap)=>{
            state.tuntapInfo = tuntap;
            state.showTuntapEdit = true;

        }
        const handleTuntapChange = () => {
            _getSignList();
        }
        const handleTuntapRefresh = ()=>{
            refreshTuntap();
            ElMessage.success('刷新成功');
        }


        let tunnelTimer = 0;
        const _getTunnelInfo = () => {
            if (globalData.value.connected) {
                getTunnelInfo(state.tunnelHashCode.toString()).then((res) => {
                    state.tunnelHashCode = res.HashCode;
                    if (res.List) {
                        state.tunnelInfos = null;
                        nextTick(() => {
                            state.tunnelInfos = res.List;
                        });
                    }
                    tunnelTimer = setTimeout(_getTunnelInfo, 200);
                }).catch(() => {
                    tunnelTimer = setTimeout(_getTunnelInfo, 200);
                });
            } else {
                tunnelTimer = setTimeout(_getTunnelInfo, 1000);
            }
        }
        const handleTunnelEdit = (tunnel)=>{
            state.tunnelInfo = tunnel;
            state.showTunnelEdit = true;
        }
        const handleTunnelChange = () => {
            _getSignList();
        }
        const handleTunnelRefresh = ()=>{
            refreshTunnel();
            ElMessage.success('刷新成功');
        }


        const _getForwardInfo = () => {
            getForwardInfo().then((res) => {
                state.forwardInfos = null;
                nextTick(() => {
                    state.forwardInfos = res;
                });
            }).catch(() => {

            });
        }
        const handleForwardEdit = (machineName)=>{
            state.forwardInfo = machineName;
            state.showForwardEdit = true;
        }
        const handleForwardChange = () => {
            _getSignList();
        }
        const handleForwardRefresh = ()=>{
            refreshForward();
            ElMessage.success('刷新成功');
        }

        const _getSignList = () => {
            state.page.Request.GroupId = globalData.value.groupid;
            getSignList(state.page.Request).then((res) => {
                state.page.Request = res.Request;
                state.page.Count = res.Count;
                for (let j in res.List) {
                    res.List[j].showTunnel = machineName.value != res.List[j].MachineName;
                    res.List[j].showForward = machineName.value != res.List[j].MachineName;
                    res.List[j].showDel = machineName.value != res.List[j].MachineName && res.List[j].Connected == false;
                    res.List[j].isSelf = machineName.value == res.List[j].MachineName;
                }
                state.page.List = res.List;
                _getForwardInfo();
            }).catch((err) => { });
        }
        const _getSignList1 = () => {
            if (globalData.value.connected) {
                state.page.Request.GroupId = globalData.value.groupid;
                getSignList(state.page.Request).then((res) => {
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
            ElMessage.success('刷新成功');  
        }
        const handlePageChange = () => {
            _getSignList();
        }
        const handleDel = (name) => {
            updateSignInDel(name).then(() => {
                _getSignList();
            });
        }

        const resizeTable = () => {
            nextTick(() => {
                state.height = wrap.value.offsetHeight - 80;
            });
        }
        onMounted(() => {
            subWebsocketState((state) => { if (state) _getSignList(); });
            resizeTable();
            window.addEventListener('resize', resizeTable);
            _getSignList();
            _getSignList1();
            _getTuntapInfo();
            _getTunnelInfo();
        });
        onUnmounted(() => {
            clearTimeout(tuntapTimer);
            clearTimeout(tunnelTimer);
            window.removeEventListener('resize', resizeTable);
        });

        return {
            machineName, state, wrap,handleDeviceEdit,handlePageRefresh, handlePageChange, handleDel,
            handleTuntapEdit, handleTuntapChange, handleTuntapRefresh,
            handleTunnelEdit, handleTunnelChange, handleTunnelRefresh,
            handleForwardEdit,handleForwardChange,handleForwardRefresh
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