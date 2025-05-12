<template>
   <el-dialog class="options-center" :title="$t('firewall.rule')" destroy-on-close v-model="state.show" width="50rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm.Data" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('firewall.srcName')" prop="SrcId">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-select v-model="state.ruleForm.Data.SrcId" @change="handleMachineChange()"
                                filterable remote :loading="state.loading" :remote-method="handleMachineSearch">
                                <template #header>
                                    <div class="t-c">
                                        <div class="page-wrap">
                                            <el-pagination small background layout="prev, pager, next" 
                                            :page-size="state.machineIds.Request.Size" 
                                            :total="state.machineIds.Count" 
                                            :pager-count="5"
                                            :current-page="state.machineIds.Request.Page" @current-change="handleMachinePageChange" />
                                        </div>
                                    </div>
                                </template>
                                <el-option v-for="(item, index) in state.machineIds.List" :key="index" :label="item.MachineName" :value="item.MachineId">
                                </el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                    
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('firewall.dstCidr')" prop="DstCIDR">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-model="state.ruleForm.Data.DstCIDR" />
                        </el-col>
                        <el-col :span="12">
                            10.18.1.1/24、10.18.1.1、0、*
                        </el-col>
                    </el-row>
                </el-form-item>
                
                <el-form-item :label="$t('firewall.dstPort')" prop="DstPort">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-model="state.ruleForm.Data.DstPort" />
                        </el-col>
                        <el-col :span="12">
                            80、80-88、80,443、0、*
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('firewall.protocol')" prop="Protocol">
                    <el-row class="w-100">
                        <el-col :span="12">
                           <el-select v-model="state.protocolChecks" multiple >
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.protocols"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('firewall.action')" prop="Action">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-select v-model="state.ruleForm.Data.Action" >
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.actions"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('firewall.orderby')" prop="OrderBy">
                     <el-input-number v-model="state.ruleForm.Data.OrderBy" :min="0" :max="65535" style="width:13rem" />
                </el-form-item>
                <el-form-item :label="$t('firewall.disabled')" prop="Disabled">
                    <div class="flex">
                        <el-switch v-model="state.ruleForm.Data.Disabled" 
                        active-text="😀" inactive-text="😣" inline-prompt />
                    </div>
                </el-form-item> 
                <el-form-item :label="$t('firewall.remark')" prop="Remark">
                    <el-input v-model="state.ruleForm.Data.Remark" />
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>

<script>
import { ElMessage } from 'element-plus';
import { inject, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import { addFirewall } from '@/apis/firewall';
import { getSignInIds } from '@/apis/signin';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();

        const add = inject('add');
        const state = reactive({
            show:true,
            loading:false,
            machineIds:{
                Request: {
                    Page: 1, Size:10, Name: ''
                },
                Count: 0,
                List: []
            },

            protocolChecks:[
                (add.value.Data.Protocol & 1) ,
                (add.value.Data.Protocol & 2)
            ].filter(c=>c > 0),
            ruleForm:{
                MachineId:add.value.MachineId,
                Data:{
                    Id:add.value.Data.Id,
                    GroupId:add.value.Data.GroupId,
                    SrcName:add.value.Data.SrcName,
                    Disabled:add.value.Data.Disabled,
                    OrderBy:add.value.Data.OrderBy,

                    SrcId:add.value.Data.SrcId,
                    DstCIDR:add.value.Data.DstCIDR,
                    DstPort:add.value.Data.DstPort,
                    Protocol:add.value.Data.Protocol,
                    Action:add.value.Data.Action,
                    Remark:add.value.Data.Remark,
                }
            },
            rules:{
                SrcId: [{ required: true, message: "required", trigger: "blur" }],
                DstCIDR: [
                    { 
                        type:'string',required: true, message: "required", trigger: "blur",
                        pattern: /^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}(\/([0-9]|([1-2][0-9])|(3[0-2])))?)$|^0$|^\*$/,
                    }
                ],
                DstPort: [
                    { 
                        type:'string',required: true, message: "required", trigger: "blur",
                        pattern: /^((\d+)(?:,(\d+))*)$|^((\d+)(?:\-(\d+))*)$|^0$|^\*$/,
                    }
                ]
            },
            protocols: [
                {label:'TCP',value:1},
                {label:'UDP',value:2}
            ],
            actions: [
                {label:t('firewall.actionAllow'),value:1},
                {label:t('firewall.actionDeny'),value:2},
            ],
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));
                json.Data.Protocol = state.protocolChecks.reduce((a,b)=>a|b,0);
                addFirewall(json).then(()=>{
                    ElMessage.success(t('common.oper'));
                    state.show = false;
                    emit('success');
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }

        const handleMachineChange = () => {
            const machine = state.machineIds.List.find(c=>c.MachineId == state.ruleForm.Data.SrcId);
            if(machine){
                state.ruleForm.Data.SrcName = machine.MachineName;
            }
        }
        const handleMachinePageChange = (page)=>{
            state.machineIds.Request.Page = page;
            _getMachineIds();
        }
        const handleMachineSearch = (name)=>{
            state.machineIds.Request.Name = name;
            _getMachineIds();
        }
        const _getMachineIds = ()=>{
            state.loading = true;
            getSignInIds(state.machineIds.Request).then((res)=>{
                state.loading = false;
                state.machineIds.Request = res.Request;
                state.machineIds.Count = res.Count;
                res.List.splice(0,0,{MachineId:'*',MachineName:'*'});

                if(state.ruleForm.Data.SrcId){
                    if(res.List.filter(c=>c.MachineId == state.ruleForm.Data.SrcId).length == 0){
                        res.List.splice(1,0,{MachineId:state.ruleForm.Data.SrcId,MachineName:state.ruleForm.Data.SrcName});
                    }
                }

                state.machineIds.List = res.List;
            }).catch((e)=>{
                state.loading = false;
            });
        }

        return {state,ruleFormRef,handleSave,handleMachineSearch,handleMachinePageChange,handleMachineChange}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>