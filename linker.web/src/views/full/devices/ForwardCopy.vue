<template>
  <el-dialog v-model="state.show" @open="handleOnShowList" append-to=".app-wrap" title="复制端口转发" top="1vh" width="500">
        <div>
            <div class="t-c head">
                <span>复制</span>
                <el-select v-model="state.machineId" @change="handleMachineChange" 
                filterable remote :loading="state.loading" :remote-method="handleSearch">
                    <template #header>
                        <div class="t-c">
                            <div class="page-wrap">
                                <el-pagination small background layout="prev, pager, next" 
                                :page-size="state.machineIds.Request.Size" 
                                :total="state.machineIds.Count" 
                                :pager-count="5"
                                :current-page="state.machineIds.Request.Page" @current-change="handlePageChange" />
                            </div>
                        </div>
                    </template>
                    <el-option v-for="(item, index) in state.machineIds.List" :key="index" :label="item.MachineName" :value="item.MachineId">
                    </el-option>
                </el-select>
                <span>到【{{ state.toMachineName }}】的端口转发记录</span>
            </div>
            <el-table :data="state.forwards" size="small" border>
                <el-table-column property="Name" label="名称"></el-table-column>
                <el-table-column prop="BufferSize" label="缓冲区" width="60">
                    <template #default="scope">
                        {{ (1<<scope.row.BufferSize) }}KB
                    </template>
                </el-table-column>
                <el-table-column property="Port" label="监听端口" width="80"></el-table-column>
                <el-table-column property="TargetEP" label="目标服务" width="140"></el-table-column>
                <el-table-column label="操作" width="80">
                    <template #default="scope">
                        <el-checkbox v-model="scope.row.use">使用</el-checkbox>
                    </template>
                </el-table-column>
            </el-table>
            <div class="foot t-c">
                <el-button type="primary" @click="handleConfirm">确定复制</el-button>
            </div>
        </div>
    </el-dialog>
</template>
<script>
import {  onMounted, onUnmounted, reactive, watch } from 'vue';
import { getForwardRemoteInfo,addForwardInfo } from '@/apis/forward'
import { ElMessage } from 'element-plus';
import {WarnTriangleFilled} from '@element-plus/icons-vue'
import { injectGlobalData } from '@/provide';
import { useForward } from './forward';
import { getSignInIds } from '@/apis/signin';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components:{WarnTriangleFilled},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const forward = useForward();
        const state = reactive({
            show: true,
            loading:false,
            machineId: '',
            toMachineId: forward.value.current,
            toMachineName: forward.value.machineName,
            machineIds:{
                Request: {
                    Page: 1, Size:10, Name: ''
                },
                Count: 0,
                List: []
            },
            forwards: []
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleOnShowList = () => {
            _getMachineIds();
        }
        const _getMachineIds = ()=>{
            state.loading = true;
            getSignInIds(state.machineIds.Request).then((res)=>{
                state.loading = false;
                state.machineIds.Request = res.Request;
                state.machineIds.Count = res.Count;
                state.machineIds.List = res.List;
                if(!state.machineId && state.machineIds.List.length > 0){
                    state.machineId = state.machineIds.List[0].MachineId;
                    _getForwardRemoteInfo();
                }
            }).catch((e)=>{
                state.loading = false;
            });
        }
        const handlePageChange = (page)=>{
            state.machineIds.Request.Page = page;
            _getMachineIds();
        }
        const handleSearch = (name)=>{
            state.machineIds.Request.Name = name;
            _getMachineIds();
        }
        const _getForwardRemoteInfo = ()=>{
            getForwardRemoteInfo({
                MachineId: state.machineId,
                ToMachineId: state.toMachineId
            }).then((res)=>{
                res.forEach(c=>{
                    c.use = true;
                });
                state.forwards = res;
            }).catch((e)=>{
                console.log(e);
            });
        }
        const handleMachineChange = ()=>{
            _getForwardRemoteInfo();
        }
        const handleConfirm = ()=>{
           const tasks =  state.forwards.filter(c=>c.use)
           .map(c=>addForwardInfo({Name:c.Name,Port:c.Port,TargetEP:c.TargetEP,BufferSize:c.BufferSize,MachineId:state.toMachineId})); 
           Promise.all(tasks).then(()=>{
                ElMessage.success('已操作!');
                state.show = false;
           }).catch(()=>{
                ElMessage.success('操作失败!');
           });
        }

        onMounted(()=>{
            _getMachineIds();
        });
        onUnmounted(()=>{
        });

        return {
            state,handleSearch, handleOnShowList,handleMachineChange,handleConfirm,handlePageChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-select{width : 12rem}
.head{padding-bottom:1rem}
.foot{padding-top:1rem}
.page-wrap{display:inline-block}
</style>