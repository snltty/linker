<template>
    <el-dialog v-model="state.show" append-to=".app-wrap" :title="state.title" top="1vh" width="400">
        <div>
            <el-descriptions border size="small" :column="1" label-width="8rem" overlength-control="wrap">
                <el-descriptions-item label="名称">{{ state.status.Info.Name }}</el-descriptions-item>
                <el-descriptions-item label="带宽">
                    <div>
                        <span v-if="state.status.Info.Bandwidth < 0">禁止</span>
                        <span v-if="state.status.Info.Bandwidth == 0">无限制</span>
                        <span v-else-if="state.status.Info.Bandwidth>0">{{state.status.Info.Bandwidth}}Mbps</span>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item label="开始时间">{{ state.status.Info.UseTime }}</el-descriptions-item>
                <el-descriptions-item label="结束时间">{{ state.status.Info.EndTime }}</el-descriptions-item>
                <el-descriptions-item label="作用于">
                    <span v-if="state.status.Info.UserId">同用户id客户端</span>
                    <span v-else-if="state.status.Info.MachineId">本客户端</span>
                </el-descriptions-item>
                <el-descriptions-item label="备注">{{ state.status.Info.Remark }}</el-descriptions-item>
               
                 <el-descriptions-item label="操作">
                    <div v-if="state.super">
                        <template v-if="state.status.Info.Id > 0">
                            <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="确定删除?" @confirm="handleDel">
                                <template #reference>
                                    <el-button type="danger" size="small" :loading="state.loading"><el-icon> <Delete /></el-icon></el-button>
                                </template>
                            </el-popconfirm>
                        </template>
                        <template v-else>
                            <el-button type="success" size="small" :loading="state.loading" @click="handleAdd"><el-icon> <Plus /></el-icon></el-button>
                        </template>
                    </div>
                </el-descriptions-item>
            </el-descriptions>
        </div>
    </el-dialog>
    <Add v-if="state.showAdd" v-model="state.showAdd" @success="handleAddSuccess"></Add>
</template>
<script>
import { computed, onMounted, provide, reactive, ref, watch } from 'vue';
import { useWlist } from './wlist';
import { wlistDel, wlistStatus } from '@/apis/wlist';
import { Delete, Plus } from '@element-plus/icons-vue';
import { injectGlobalData } from '@/provide';
import Add from './Add.vue';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {Delete, Plus,Add },
    setup(props, { emit }) {
        const wlist = useWlist();
        const globalData = injectGlobalData();
        
        const state = reactive({
            show: true,
            machineId: wlist.value.device.id,
            title: `[${wlist.value.device.name}]上的${wlist.value.device.typeText}白名单`,
            status: {
                Enabled:false,
                Type:'',
                Info:{}
            },
            super:computed(()=>globalData.value.signin.Super),
            loading:false,
            showAdd:false
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleRefreshData = ()=>{
            wlistStatus(wlist.value.device.type,wlist.value.device.id).then(res=>{

                state.status = res;
                state.status.Info = res.Info || {}
            });
        }
        const handleDel = ()=>{
            state.loading = true;
            wlistDel(state.status.Info.Id).then(()=>{
                state.loading = false;
                handleRefreshData();
                emit('change');
            }).catch(()=>{
                state.loading = false;
            })
        }

        const editState = ref({});
        const nodes = ref([]);
        provide('edit',editState);
        provide('nodes',nodes);
        const handleAdd = ()=>{
            editState.value = {
                Id:0
                ,Name:wlist.value.device.name
                ,Nodes:['*']
                ,Remark:''
                ,MachineId:wlist.value.device.id
                ,Type:wlist.value.device.type
                ,prefix:''
            };
            state.showAdd = true;
        }
        const handleAddSuccess = ()=>{
            handleRefreshData();
            emit('change');
        }

        onMounted(()=>{
            handleRefreshData();
        });

        return {
            state,handleRefreshData,handleDel,handleAdd,handleAddSuccess
        }
    }
}
</script>
<style lang="stylus" scoped>

</style>