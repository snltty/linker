<template>
    <el-dialog append-to=".app-wrap" v-model="state.show" :title="state.title" top="1vh" width="400">
        <div>
            <el-descriptions border size="small" :column="1" label-width="8rem" overlength-control="wrap">
                <el-descriptions-item :label="$t('wlist.name')">{{ state.status.Info.Name }}</el-descriptions-item>
                <el-descriptions-item :label="$t('wlist.bandwidth')">
                    <div>
                        <span v-if="state.status.Info.Bandwidth < 0">{{$t('wlist.deny',[''])}}</span>
                        <span v-if="state.status.Info.Bandwidth == 0">{{$t('wlist.allow')}}</span>
                        <span v-else-if="state.status.Info.Bandwidth>0">{{state.status.Info.Bandwidth}}Mbps</span>
                    </div>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('wlist.usetime')">{{ state.status.Info.UseTime }}</el-descriptions-item>
                <el-descriptions-item :label="$t('wlist.endtime')">{{ state.status.Info.EndTime }}</el-descriptions-item>
                <el-descriptions-item :label="$t('wlist.target')">
                    <span v-if="state.status.Info.UserId">{{$t('wlist.userid')}}</span>
                    <span v-else-if="state.status.Info.MachineId">{{$t('wlist.machineid')}}</span>
                </el-descriptions-item>
                <el-descriptions-item :label="$t('wlist.remark')">{{ state.status.Info.Remark }}</el-descriptions-item>
               
                 <el-descriptions-item :label="$t('common.oper')">
                    <div v-if="state.super">
                        <template v-if="state.status.Info.Id > 0">
                            <el-popconfirm 
                            :confirm-button-text="$t('common.confirm')"
                            :cancel-button-text="$t('common.cancel')" 
                            :title="$t('common.delSure',[''])" @confirm="handleDel">
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
import { useI18n } from 'vue-i18n';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {Delete, Plus,Add },
    setup(props, { emit }) {

        const {t} = useI18n();
        const wlist = useWlist();
        const globalData = injectGlobalData();
        
        const state = reactive({
            show: true,
            machineId: wlist.value.device.id,
            title:t('wlist.title',[wlist.value.device.name,wlist.value.device.typeText]),
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