<template>
    <div class="home-list-wrap absolute" ref="wrap">
        <el-table :data="state.page.List" border style="width: 100%" :height="`${state.height}px`" size="small">
            <el-table-column prop="MachineName" label="设备">
                <template #default="scope">
                    <div>
                        <template v-if="scope.row.Connected">
                            <strong class="green">{{ scope.row.MachineName }}</strong>
                        </template>
                        <template v-else>
                            <span>{{ scope.row.MachineName }}</span>
                        </template>
                    </div>
                </template>
            </el-table-column>
            <el-table-column prop="IP" label="IP" width="160" />
            <el-table-column prop="Version" label="版本" width="60" />
            <el-table-column prop="LastSignIn" label="最后登入" width="140" />
            <el-table-column label="操作">
                <template #default="scope">
                    <el-button size="small" @click="handleTestTunnel(scope.row.MachineName)">测试</el-button>
                    <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="删除不可逆，是否确认?" @confirm="handleDelTunnel(scope.row.MachineName)">
                        <template #reference>
                            <el-button type="danger" size="small">删除</el-button>
                        </template>
                    </el-popconfirm>
                </template>
            </el-table-column>
        </el-table>
        <div class="page t-c">
            <div class="page-wrap">
                <el-pagination small background layout="prev, pager, next"  
                :total="state.page.Count" :page-size="state.page.Request.Size" :current-page="state.page.Request.Page"
                @current-change="handlePageChange"/>
            </div>
        </div>
    </div>
</template>
<script>
import {getSignList,updateTunnelConnect,updateSignInDel} from '@/apis/tunnel.js'
import {subWebsocketState} from '@/apis/request.js'
import {injectGlobalData} from '../provide.js'
import {reactive,onMounted, ref, nextTick, onUnmounted} from 'vue'
export default {
    setup(props) {
        
        const globalData = injectGlobalData();
        const wrap = ref(null);
        const state = reactive({
            page:{
                Request:{Page:1,Size:10,GroupId:globalData.value.groupid},
                Count:0,
                List:[]
            },
            height:0,
        });
        
        const _getSignList = ()=>{
            getSignList(state.page.Request).then((res)=>{
                state.page.Request = res.Request;
                state.page.Count = res.Count;
                state.page.List = res.List;
            }).catch((err)=>{});
        }
        const handlePageChange = ()=>{
            _getSignList();
        }
        const resizeTable = ()=>{
            nextTick(()=>{
                state.height = wrap.value.offsetHeight - 80;
            });
        }
        const handleTestTunnel = (name)=>{
            updateTunnelConnect(name);
        }
        const handleDelTunnel = (name)=>{
            updateSignInDel(name).then(()=>{
                _getSignList();
            });
        }

        onMounted(()=>{
            subWebsocketState((state)=>{ if(state)_getSignList();});
            resizeTable();
            window.addEventListener('resize',resizeTable);
        });
        onUnmounted(()=>{
            window.removeEventListener('resize',resizeTable);
        });

        return {
            state,wrap,handlePageChange,handleTestTunnel,handleDelTunnel
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