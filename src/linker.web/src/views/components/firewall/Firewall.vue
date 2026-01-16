<template>
    <div class="h-100 flex flex-column flex-nowrap">
        <div class="head">
            <div class="flex">
                <div class="flex mgt-1">
                    <div>
                        <el-select v-model="state.search.Data.Action" @change="loadData" size="small" class="mgr-1" style="width: 9rem;">
                            <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.actions"></el-option>
                        </el-select>
                    </div>
                    <div>
                        <el-select v-model="state.search.Data.Protocol" @change="loadData" size="small" class="mgr-1" style="width: 9rem;">
                            <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.protocols"></el-option>
                        </el-select>
                    </div>
                    <div>
                        <el-select v-model="state.search.Data.Disabled" @change="loadData" size="small" class="mgr-1" style="width: 9rem;">
                            <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.states"></el-option>
                        </el-select>
                    </div>
                </div>
                <div class="flex mgt-1">
                    <div>
                        <span>{{$t('firewall.srcName')}}/{{$t('firewall.dstCidr')}}/{{$t('firewall.dstPort')}}/{{$t('firewall.remark')}}</span>
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
                <div class="mgt-1" v-if="state.isSelf">
                    <Sync name="Firewall"></Sync>
                </div>
            </div>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table class="firewall" stripe border :data="state.data" size="small" :row-class-name="tableRowClassName" height="100%">
                    <el-table-column prop="Checked" width="30" v-if="state.isSelf" >
                        <template #header>
                            <el-checkbox size="small" v-model="state.checkAll" :indeterminate="state.checkAllIndeterminate" @change="handleCheckAllChange"></el-checkbox>
                        </template>
                        <template #default="scope">
                            <el-checkbox v-model="scope.row.Checked" size="small" @change="handleChecked(scope.row)"></el-checkbox>
                        </template>
                    </el-table-column>
                    <el-table-column prop="SrcName" :label="$t('firewall.srcName')" >
                        <template v-slot="scope">
                            <div class="ellipsis" :title="scope.row.SrcName">{{ scope.row.SrcName }}</div>
                        </template>
                    </el-table-column>
                    <el-table-column prop="DstCIDR" :label="$t('firewall.dstCidr')" width="130"></el-table-column>
                    <el-table-column prop="DstPort" :label="$t('firewall.dstPort')"></el-table-column>
                    <el-table-column prop="Protocol" :label="$t('firewall.protocol')" width="70">
                        <template v-slot="scope">{{handleShowProtocol(scope.row.Protocol)}}</template>
                    </el-table-column>
                    <el-table-column prop="Action" :label="$t('firewall.action')" width="56">
                        <template v-slot="scope">{{handleShowAction(scope.row.Action)}}</template>
                    </el-table-column>
                    <el-table-column prop="OrderBy" :label="$t('firewall.orderby')" width="56"></el-table-column>
                    <el-table-column prop="Disabled" :label="$t('firewall.disabled')" width="66">
                        <template v-slot="scope">
                            <div>
                                <el-switch v-model="scope.row.Disabled" size="small" 
                                active-text="😀" inactive-text="😣" inline-prompt @change="handleDsiabled(scope.row)"
                                    style="--el-switch-on-color: red;" />
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Remark" :label="$t('firewall.remark')" >
                        <template v-slot="scope">
                            <div class="ellipsis" :title="scope.row.Remark">{{ scope.row.Remark }}</div>
                        </template>
                    </el-table-column>
                    <el-table-column width="60" fixed="right">
                        <template #header>
                            <div class="flex">
                                <el-switch v-model="state.state" size="small" :title="$t('firewall.switch')" 
                                :active-value="0" :inactive-value="1" 
                                active-text="😀" inactive-text="😣" inline-prompt @change="handleSetState" />
                            </div>
                        </template>
                        <template #default="scope">
                            <div>
                                <el-dropdown>
                                    <span class="el-dropdown-link">{{$t('common.option')}}<el-icon><ArrowDown /></el-icon></span>
                                    <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item @click="handleAdd(scope.row)">{{$t('firewall.edit')}}</el-dropdown-item>
                                        <el-dropdown-item @click="handleDel(scope.row)">{{$t('firewall.del')}}</el-dropdown-item>
                                    </el-dropdown-menu>
                                    </template>
                                </el-dropdown>
                            </div>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
    <Add v-if="state.showAdd" v-model="state.showAdd" @success="loadData"></Add>
</template>

<script>
import { reactive,computed, ref } from '@vue/reactivity'
import {  onMounted, provide } from '@vue/runtime-core'
import { injectGlobalData } from '@/provide'
import { addFirewall, checkFirewall, getFirewall, removeFirewall, stateFirewall } from '@/apis/firewall';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import {ArrowDown} from'@element-plus/icons-vue'
import Add from './Add.vue';
import Sync from '../sync/Index.vue'
export default {
    props: ['machineId','machineName'],
    components:{Add,Sync,ArrowDown},
    setup(props,{emit}) {

        const {t} = useI18n();

        const globalData = injectGlobalData();
        const state = reactive({
            loading: true,
            checkAll:false,
            checkAllIndeterminate:false,
            search:{
                MachineId:props.machineId || globalData.value.config.Client.Id,
                Data:{
                    Str:'',
                    Disabled:-1,
                    Protocol: 3,
                    Action:3,
                }
            },
            protocols: [
                {label:t('firewall.protocolall'),value:3},
                {label:'TCP',value:1},
                {label:'UDP',value:2}
            ],
            actions: [
                {label:t('firewall.actionall'),value:3},
                {label:t('firewall.actionAllow'),value:1},
                {label:t('firewall.actionDeny'),value:2},
            ],
            states: [
                {label:t('firewall.disabledAll'),value:-1},
                {label:t('firewall.enabled'),value:0},
                {label:t('firewall.disabled'),value:1},
            ],

            data:[],
            state:1,
            showAdd:false,
            isSelf:computed(()=>{
                return state.search.MachineId == globalData.value.config.Client.Id;
            })
        })
        const loadData = () => {
            state.loading = true;
            getFirewall(state.search).then((res) => {
                state.loading = false;
                state.data = res.List;
                state.state = res.State;
                checkAllStatus();
            }).catch((err) => {
                console.log(err);
                state.loading = false;
            });
        }

        const checkAllStatus = ()=>{
            state.checkAll = state.data.some(item=>item.Checked);
            state.checkAllIndeterminate = state.checkAll && state.data.every(item=>item.Checked) == false;
        }
        const handleCheckAllChange = (value)=>{
            state.data.forEach(c=>{
                c.Checked = value;
            });
            checkAllStatus();

            const ids = state.data.map(item=>item.Id);
            if(ids.length > 0){
                _checkFirewall(ids,value);
            }
        }
        const handleChecked = (value)=>{
            checkAllStatus();
            _checkFirewall([value.Id],value.Checked);
        }
        const _checkFirewall = (ids,value)=>{
            state.loading = true;
            checkFirewall({
                Ids:ids,
                IsChecked:value
            }).then(()=>{state.loading = false;}).catch(()=>{state.loading = false;});
        }

        const handleSetState = ()=>{
            state.loading = true;
            stateFirewall({
                MachineId:state.search.MachineId,
                State:state.state
            }).then(()=>{
                state.loading = false;
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                state.loading = false;
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }
        const handleDel = (row) => {
            ElMessageBox.confirm(t('firewall.delConfirm'), t('common.confirm'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText: t('common.cancel'),
                type: 'warning',
            }).then(() => {
                state.loading = true;
                removeFirewall({
                    MachineId:state.search.MachineId,
                    Id:row.Id
                }).then(()=>{loadData(); state.loading = false;}).catch(()=>{state.loading = false})
            }).catch(()=>{});
        }
        const handleDsiabled = (row)=>{
             state.loading = true;
            addFirewall({
                MachineId:state.search.MachineId,
                Data:row,
            }).then(()=>{loadData(); state.loading = false;}).catch(()=>{state.loading = false;});
        }

        const addState = ref({});
        provide('add',addState);
        const handleAdd = (row)=>{
            addState.value = {
                MachineId:state.search.MachineId,
                Data: row || {
                    Id:'',
                    GroupId:globalData.value.config.Client.Group.Id,
                    SrcName:'',
                    Disabled:false,
                    OrderBy:0,

                    SrcId:'',
                    DstCIDR:'0.0.0.0/0',
                    DstPort:'0',
                    Protocol:3,
                    Action:1,
                    Remark:t('firewall.actionAllowAll')
                }
            };
            state.showAdd = true;
        }
        
        const protocolArr = ['','TCP','UDP'];
        const handleShowProtocol = (protocol)=>{
            return [
                protocolArr[(protocol & 1)] ,
                protocolArr[(protocol & 2)]
            ].filter(c=>!!c).join('/');
        }
        const actionArr = ['','✔','✘'];
        const handleShowAction = (action)=>{
            return actionArr[action];
        }
       

        const tableRowClassName = ({ row, rowIndex }) => {
            return `action-${row.Action}`;
        }
        onMounted(()=>{
            loadData();
        });

        return {
            state, loadData, tableRowClassName,handleSetState,handleAdd,handleDel,
            handleShowProtocol,handleShowAction,handleDsiabled,
            handleCheckAllChange,handleChecked
        }
    }
}
</script>
<style lang="stylus" scoped>
.head {
    color:#555;
    border:1px solid #eee;
    padding:0 1rem 1rem 1rem;
    border-bottom:0;
}
html.dark .head{border-color:#575c61;}
</style>
<style  lang="stylus">
.firewall.el-table {
    .action-1 {
        color: green;
    }

    .action-2 {
        color: #c83f08;
    }
}

</style>