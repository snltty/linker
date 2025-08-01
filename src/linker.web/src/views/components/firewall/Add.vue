<template>
   <el-dialog class="options-center" :title="$t('firewall.rule')" destroy-on-close v-model="state.show" width="50rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm.Data" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('firewall.srcName')" prop="SrcId">
                    <el-input v-trim type="textarea" v-model="state.ruleForm.Data.SrcName" @click="handleSrcId" readonly resize="none" rows="2"></el-input>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('firewall.dstCidr')" prop="DstCIDR">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-trim v-model="state.ruleForm.Data.DstCIDR" />
                        </el-col>
                        <el-col :span="12">
                            10.18.1.1/24、10.18.1.1、0、*
                        </el-col>
                    </el-row>
                </el-form-item>
                
                <el-form-item :label="$t('firewall.dstPort')" prop="DstPort">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-trim v-model="state.ruleForm.Data.DstPort" />
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
                    <el-input v-trim v-model="state.ruleForm.Data.Remark" maxlength="64" show-word-limit/>
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
    <el-dialog class="options-center" :title="$t('firewall.srcName')" destroy-on-close v-model="state.showSrcName" width="54rem" top="2vh">
        <div>
            <el-transfer class="src-tranfer"
                v-model="state.srcIdValues"
                filterable
                :filter-method="srcFilterMethod"
                :data="state.srcIds"
                :titles="[$t('firewall.unselect'), $t('firewall.selected')]"
                :props="{
                    key: 'MachineId',
                    label: 'MachineName',
                }"
            />
            <div class="t-c w-100 mgt-1">
                    <el-button @click="state.showSrcName = false">{{$t('common.cancel')}}</el-button>
                    <el-button type="primary" @click="handleSrcName">{{$t('common.confirm')}}</el-button>
                </div>
        </div>
    </el-dialog>
</template>

<script>
import { ElMessage } from 'element-plus';
import { inject, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import { addFirewall } from '@/apis/firewall';
import { getSignInIds, getSignInNames } from '@/apis/signin';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();

        const add = inject('add');
        const state = reactive({
            show:true,
            loading:false,

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

            srcIds: [],
            srcIdValues:[],
            showSrcName:false,
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


        const handleSrcId = ()=>{
            state.srcIdValues = state.ruleForm.Data.SrcId.split(',').filter(c=>c);
            state.showSrcName = true;
        }
        const handleSrcName = ()=>{
            state.ruleForm.Data.SrcId = state.srcIdValues.join(',');
            state.ruleForm.Data.SrcName = state.srcIds.filter(c=>state.srcIdValues.includes(c.MachineId)).map(c=>c.MachineName).join(',');
            state.showSrcName = false;
        }
        const _getSignInNames = ()=>{
            state.loading = true;
            getSignInNames().then((res)=>{
                state.loading = false;
                res.splice(0,0,{MachineId:'*',MachineName:'*'});

                state.srcIds = res;
            }).catch((e)=>{
                state.loading = false;
            });
        }
        const srcFilterMethod = (query, item) => {
             return item.MachineName.toLowerCase().includes(query.toLowerCase())
        }
        onMounted(()=>{
            _getSignInNames();
        });

        return {state,ruleFormRef,handleSave,srcFilterMethod,handleSrcId,handleSrcName}
    }
}
</script>
<style lang="stylus">
.el-transfer.src-tranfer .el-transfer__buttons .el-button{display:block;}
.el-transfer.src-tranfer .el-transfer__buttons .el-button:nth-child(2){margin:1rem 0 0 0;}
</style>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
</style>