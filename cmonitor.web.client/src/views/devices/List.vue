<template>
    <div class="home-list-wrap absolute" ref="wrap">
        <el-table :data="state.page.List" border style="width: 100%" :height="`${state.height}px`" size="small">
            <el-table-column prop="MachineName" label="设备">
                <template #default="scope">
                    <div>
                        <p>
                            <template v-if="scope.row.Connected">
                                <strong class="green">{{ scope.row.MachineName }}</strong>
                            </template>
                            <template v-else>
                                <span>{{ scope.row.MachineName }}</span>
                            </template>
                        </p>
                        <p>{{scope.row.IP}}</p>
                    </div>
                </template>
            </el-table-column>
            <el-table-column prop="tunel" label="通道" width="90" >
                <template #default="scope">
                    <div v-if="machineName != scope.row.MachineName">
                        <Tunnel :data="scope.row"></Tunnel>
                    </div>
                </template>
            </el-table-column>
            <el-table-column prop="tuntap" label="网卡"  width="180">
                <template #default="scope">
                    <template v-if="state.tuntapInfos[scope.row.MachineName]">
                        <Tuntap @change="handleTuntapChange" :data="state.tuntapInfos[scope.row.MachineName]"></Tuntap>
                    </template>
                </template>
            </el-table-column>
            <el-table-column prop="LastSignIn" label="其它" width="140" >
                <template #default="scope">
                    <div>
                        <p>{{scope.row.LastSignIn}}</p>
                        <p>v{{scope.row.Version}}</p>
                    </div>
                </template>
            </el-table-column>
            <el-table-column label="操作">
                <template #default="scope">
                    <el-popconfirm v-if="machineName != scope.row.MachineName" confirm-button-text="确认" cancel-button-text="取消" title="删除不可逆，是否确认?" @confirm="handleDel(scope.row.MachineName)">
                        <template #reference>
                            <el-button type="danger" size="small">删除</el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="total,prev, pager, next"  
                :total="state.page.Count" :page-size="state.page.Request.Size" :current-page="state.page.Request.Page"
                @current-change="handlePageChange"/>
            </div>
        </div>
    </div>
</template>
<script>
import {getSignList,updateSignInDel} from '@/apis/signin.js'
import {subWebsocketState} from '@/apis/request.js'
import {getTuntapInfo} from '@/apis/tuntap'
import {injectGlobalData} from '@/provide.js'
import {reactive,onMounted, ref, nextTick, onUnmounted, computed} from 'vue'
import { ElMessage } from 'element-plus'
import Tuntap from './Tuntap.vue'
import Tunnel from './Tunnel.vue'
export default {
    components:{Tuntap,Tunnel},
    setup(props) {
        
        const globalData = injectGlobalData();
        const machineName = computed(()=>globalData.value.config.Client.Name);
        const wrap = ref(null);
        const state = reactive({
            page:{
                Request:{Page:1,Size:10,GroupId:globalData.value.groupid},
                Count:0,
                List:[]
            },
            tuntapInfos:{},
            tuntapHashCode:0,
            height:0,
        });

        let tuntapTimer = 0;
        const _getTuntapInfo = ()=>{
            if(globalData.value.connected){
                getTuntapInfo(state.tuntapHashCode.toString()).then((res)=>{
                    state.tuntapHashCode = res.HashCode;
                    if(res.List){  
                        state.tuntapInfos = {};
                        nextTick(()=>{
                            state.tuntapInfos = res.List.reduce((json,value,index)=>{
                                json[value.MachineName] = value;
                                json[value.MachineName].running =  value.Status == 2;
                                json[value.MachineName].loading =  value.Status == 1;
                                return json;
                            },{});
                        });
                    }
                    tuntapTimer = setTimeout(_getTuntapInfo,200);
                }).catch(()=>{
                    tuntapTimer = setTimeout(_getTuntapInfo,200);
                });
            }else{
                tuntapTimer = setTimeout(_getTuntapInfo,1000);
            }
        }

        const _getSignList = ()=>{
            state.page.Request.GroupId = globalData.value.groupid;
            getSignList(state.page.Request).then((res)=>{
                state.page.Request = res.Request;
                state.page.Count = res.Count;
                state.page.List = res.List;
            }).catch((err)=>{});
        }
        const handlePageChange = ()=>{
            _getSignList();
        }
        const handleDel = (name)=>{
            updateSignInDel(name).then(()=>{
                _getSignList();
            });
        }
        const handleTuntapChange = ()=>{
            _getSignList();
        }

        const resizeTable = ()=>{
            nextTick(()=>{
                state.height = wrap.value.offsetHeight - 80;
            });
        }
        onMounted(()=>{
            subWebsocketState((state)=>{ if(state)_getSignList();});
            resizeTable();
            window.addEventListener('resize',resizeTable);
            _getSignList();
            _getTuntapInfo();
        });
        onUnmounted(()=>{
            clearTimeout(tuntapTimer);
            window.removeEventListener('resize',resizeTable);
        });

        return {
            machineName,state,wrap,handlePageChange,handleDel,handleTuntapChange
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