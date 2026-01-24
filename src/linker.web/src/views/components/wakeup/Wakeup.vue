<template>
    <div class="h-100 flex flex-column flex-nowrap">
        <div class="head">
            <div class="flex">
                <div class="flex mgt-1">
                    <div>
                        <el-select v-model="state.search.Data.Type" @change="loadData" size="small" class="mgr-1" style="width: 9rem;">
                            <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.types"></el-option>
                        </el-select>
                    </div>
                </div>
                <div class="flex mgt-1">
                    <div>
                        <span>{{$t('wakeup.name')}}/{{$t('wakeup.value')}}/{{$t('wakeup.remark')}}</span>
                        <el-input v-trim v-model="state.search.Data.Str" @change="loadData" size="small" style="width:7rem"></el-input>
                    </div>
                    <div class="mgl-1">
                        <el-button size="small" :loading="state.loading" @click="loadData">{{$t('common.refresh')}}</el-button>
                    </div>
                    <div class="mgl-1">
                        <el-button type="success" size="small" :loading="state.loading" @click="handleAdd()">+</el-button>
                    </div>
                </div>
                <div class="flex-1"></div>
            </div>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table class="firewall" stripe border :data="state.data" size="small" height="100%">
                    <el-table-column prop="Type" :label="$t('wakeup.type')" width="70">
                        <template v-slot="scope">{{$t(`wakeup.type${scope.row.Type}`)}}</template>
                    </el-table-column>
                    <el-table-column prop="Name" :label="$t('wakeup.name')" width="100"></el-table-column>
                    <el-table-column prop="Value" :label="$t('wakeup.value')">
                        <template v-slot="scope">
                            <div class="ellipsis" :title="scope.row.Value">{{ scope.row.Value }}</div>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Content" :label="$t('wakeup.value')"></el-table-column>
                    <el-table-column prop="Remark" :label="$t('wakeup.remark')" width="100">
                        <template v-slot="scope">
                            <div class="ellipsis" :title="scope.row.Remark">{{ scope.row.Remark }}</div>
                        </template>
                    </el-table-column>
                    <el-table-column width="106" fixed="right">
                        <template #default="scope">
                            <div>
                                <template v-if="scope.row.Running">
                                    <a href="javascript:void(0);" class="a-line mgr-1 run-btn">
                                        <img src="@/assets/loading.svg" alt="run">
                                    </a>
                                </template>
                                <template v-else>
                                    <a href="javascript:void(0);" class="a-line mgr-1 run-btn" @click="handleRun(scope.row)">
                                        <img src="@/assets/run.svg" alt="run">
                                    </a>
                                </template>
                                <a href="javascript:void(0);" class="a-line mgr-1" @click="handleAdd(scope.row)">{{$t('wakeup.edit')}}</a>
                                <el-popconfirm 
                                :confirm-button-text="$t('common.confirm')" :cancel-button-text="$t('common.cancel')"
                                    :title="$t('wakeup.delConfirm')" @confirm="handleDel(scope.row)">
                                    <template #reference>
                                        <a href="javascript:void(0);" class="a-line">{{$t('wakeup.del')}}</a>
                                    </template>
                                </el-popconfirm>
                            </div>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
    <Add v-if="state.showAdd" v-model="state.showAdd" @success="loadData"></Add>
    <el-dialog v-model="state.showSwitch" :title="`${$t('wakeup.runSwitchConfirm')}【${state.switchRow.Name}】？`" width="300">
        <div class="t-c">
            <div class="mgt-2"><el-button size="large" @click="handleSwitchMs(10000)">{{ $t('wakeup.runSwitchLong') }}10000ms</el-button></div>
            <div class="mgt-2"><el-button size="large" type="success" @click="handleSwitchMs(1000)">{{ $t('wakeup.runSwitchTouch') }}1000ms</el-button></div>
            <div class="mgt-2"><el-button size="large" type="info" @click="handleSwitchCustom">{{ $t('wakeup.runSwitchCustom') }}</el-button></div>
        </div>
    </el-dialog>
</template>

<script>
import { reactive,computed, ref } from '@vue/reactivity'
import {  onMounted, provide } from '@vue/runtime-core'
import { injectGlobalData } from '@/provide'
import { getWakeup, removeWakeup, sendWakeup } from '@/apis/wakeup';
import { useI18n } from 'vue-i18n';
import Add from './Add.vue';
import { ElMessage, ElMessageBox } from 'element-plus';
export default {
    props: ['machineId','machineName'],
    components:{Add},
    setup(props,{emit}) {

        const {t} = useI18n();

        const globalData = injectGlobalData();
        const state = reactive({
            loading: true,
            search:{
                MachineId:props.machineId || globalData.value.config.Client.Id,
                Data:{
                    Str:'',
                    Type:7,
                }
            },
            types: [
                {label:t('wakeup.typeall'),value:7},
                {label:t('wakeup.typeWol'),value:1},
                {label:t('wakeup.typeCom'),value:2},
                {label:t('wakeup.typeHid'),value:4},
            ],

            data:[],
            showAdd:false,

            showSwitch:false,
            switchRow:{},
        })
        const loadData = () => {
            state.loading = true;
            getWakeup(state.search).then((res) => {
                state.loading = false;
                state.data = res;
            }).catch((err) => {
                console.log(err);
                state.loading = false;
            });
        }
        const handleDel = (row) => {
            state.loading = true;
            removeWakeup({
                MachineId:state.search.MachineId,
                Id:row.Id
            }).then(()=>{loadData(); state.loading = false;}).catch(()=>{state.loading = false;});
        }

        const handleRun = (row)=>{
            if(row.Type == 1){
                ElMessageBox.confirm(`${t('wakeup.runWolConfirm')}【${row.Name}】？`, t('common.tips'), {
                    confirmButtonText:  t('common.confirm'),
                    cancelButtonText:  t('common.cancel'),
                    type: 'warning',
                }).then(() => {
                    handleSend(row,0);
                }).catch(()=>{});
            }else if(row.Type == 2 || row.Type == 4){
                state.switchRow = row;
                state.showSwitch = true;
            }
        }
        const handleSend = (row,ms) => { 
            sendWakeup({
                MachineId:state.search.MachineId,
                Data:{
                    Id:  row.Id,
                    Value:  row.Value,
                    Content:  row.Content,
                    Type:  row.Type,
                    Ms:ms
                }
            }).then(res => {
                ElMessage.success(t('common.oper'));
                state.showSwitch = false;
            }).catch(() => {ElMessage.success(t('common.operFail'));});
        }
        const handleSwitchMs = (ms) => {
            handleSend(state.switchRow,ms);
        }
        const handleSwitchCustom = () => { 
             ElMessageBox.prompt(`${t('wakeup.runSwitchCustom')}ms`, t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                inputPattern:/^[1-9]{1,}\d{0,}$/,
                inputValue:'1000',
            }).then(({ value }) => {
                handleSend(state.switchRow,+value);
            }).catch(() => {});
        }

        const addState = ref({});
        provide('add',addState);
        const handleAdd = (row)=>{
            addState.value = {
                MachineId:state.search.MachineId,
                Data: row || {
                    Id:'',
                    Type:1,
                    Name:'',
                    Value:'',
                    Remark:'',
                }
            };
            state.showAdd = true;
        }
        onMounted(()=>{
            loadData();
        });

        return {
            state, loadData,handleAdd,handleDel,handleRun,handleSend,handleSwitchMs,handleSwitchCustom
        }
    }
}
</script>
<style lang="stylus" scoped>
html.dark .head{border-color:#575c61;}
.head {
    color:#555;
    border:1px solid #eee;
    padding:0 1rem 1rem 1rem;
    border-bottom:0;
}
</style>
<style  lang="stylus">
.firewall.el-table {

    .run-btn{
        img{height:2rem;vertical-align: middle;}
    }
}
</style>