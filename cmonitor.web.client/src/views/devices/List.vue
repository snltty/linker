<template>
    <div class="home-list-wrap absolute" ref="wrap">
        <el-table :data="state.page.List" border style="width: 100%" :height="`${state.height}px`" size="small">
            <el-table-column prop="MachineName" label="设备">
                <template #default="scope">
                    <div>
                        <p>
                            <strong :class="{green:scope.row.Connected}" >{{scope.row.MachineName }}</strong>
                        </p>
                        <p>{{ scope.row.IP }}</p>
                    </div>
                </template>
            </el-table-column>
            <el-table-column prop="tuntap" label="虚拟网卡" width="170">
                <template #default="scope">
                    <template v-if="state.tuntapInfos[scope.row.MachineName]">
                        <Tuntap @change="handleTuntapChange" :data="state.tuntapInfos[scope.row.MachineName]"></Tuntap>
                    </template>
                </template>
            </el-table-column>
            <el-table-column prop="forward" label="端口转发">
                <template #default="scope">
                    <template v-if="state.forwardInfos">
                        <template v-if="scope.row.showForward">
                            <Forward @change="handleForwardChange" :data="state.forwardInfos[scope.row.MachineName]"
                            :name="scope.row.MachineName"></Forward>
                        </template>
                        <template v-else>--</template>
                    </template>
                </template>
            </el-table-column>
            <el-table-column prop="LastSignIn" label="其它" width="140">
                <template #default="scope">
                    <div>
                        <p>{{ scope.row.LastSignIn }}</p>
                        <p>v{{ scope.row.Version }}</p>
                    </div>
                </template>
            </el-table-column>
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
    </div>
</template>
<script>
import { getSignList, updateSignInDel } from '@/apis/signin.js'
import { subWebsocketState } from '@/apis/request.js'
import { getTuntapInfo } from '@/apis/tuntap'
import { injectGlobalData } from '@/provide.js'
import { reactive, onMounted, ref, nextTick, onUnmounted, computed } from 'vue'
import Tuntap from './Tuntap.vue'
import Forward from './Forward.vue'
import { getForwardInfo } from '@/apis/forward'
export default {
    components: { Tuntap,  Forward },
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
            tuntapInfos: {},
            tuntapHashCode: 0,
            forwardInfos: null,
            height: 0,
        });

        let tuntapTimer = 0;
        const _getTuntapInfo = () => {
            if (globalData.value.connected) {
                getTuntapInfo(state.tuntapHashCode.toString()).then((res) => {
                    state.tuntapHashCode = res.HashCode;
                    if (res.List) {
                        state.tuntapInfos = {};
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
        const _getForwardInfo = () => {
            getForwardInfo().then((res) => {
                state.forwardInfos = null;
                nextTick(() => {
                    state.forwardInfos = res;
                });
            }).catch(() => {

            });
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
                        }
                    }
                    setTimeout(_getSignList1, 3000);
                }).catch((err) => { 
                    setTimeout(_getSignList1, 3000);
                });
            }else{
                setTimeout(_getSignList1, 3000);
            }
        }


        const handlePageChange = () => {
            _getSignList();
        }
        const handleDel = (name) => {
            updateSignInDel(name).then(() => {
                _getSignList();
            });
        }
        const handleTuntapChange = () => {
            _getSignList();
        }
        const handleForwardChange = () => {
            _getSignList();
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
        });
        onUnmounted(() => {
            clearTimeout(tuntapTimer);
            window.removeEventListener('resize', resizeTable);
        });

        return {
            machineName, state, wrap, handlePageChange, handleDel, handleTuntapChange, handleForwardChange
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