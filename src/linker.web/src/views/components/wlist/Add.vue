<template>
   <el-dialog class="options-center" :title="$t('server.wlist')" destroy-on-close v-model="state.show" width="40rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item label="">
                    <el-row class="w-100">
                        <el-col :span="14">
                            <el-form-item :label="$t('server.wlistMachineId')" prop="MachineId">
                                <el-select v-model="state.ruleForm.MachineId" filterable remote :loading="state.loading" :remote-method="handleMachineIds" @change="handleMachineIdChange">
                                    <el-option v-for="(item, index) in state.machineids" :key="index" :label="item.MachineName" :value="item.MachineId">
                                    </el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                        <el-col :span="10">
                            <el-form-item label-width="10">
                                <el-checkbox v-model="state.apply2user">{{$t('server.wlistUserId')}}</el-checkbox>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item :label="$t('server.wlistName')" prop="Name">
                    <el-input v-trim v-model="state.ruleForm.Name" />
                </el-form-item>
                <el-form-item :label="$t(`server.wlistNodes`)" prop="Nodes">
                    <el-input type="textarea" :value="state.nodesText" @click="handleShowNodes" readonly resize="none" :rows="4"></el-input>
                </el-form-item>
                <el-form-item v-if="state.prefix" :label="$t(`server.wlistNodes${state.ruleForm.Type}`)" prop="Domain">
                    <el-input v-trim type="textarea" v-model="state.ports" resize="none" :rows="2" @change="handlePortChange"></el-input>
                </el-form-item>
                <el-form-item :label="$t('server.wlistRangeTime')" prop="RangeTime">
                    <el-date-picker
                        v-model="state.timeRange"
                        type="daterange"
                        range-separator="->"
                        :start-placeholder="$t('server.wlistUseTime')"
                        :end-placeholder="$t('server.wlistEndTime')"
                    />
                </el-form-item>
                <el-form-item :label="$t('server.wlistBandwidth')" prop="Bandwidth">
                    <el-input-number v-model="state.ruleForm.Bandwidth" width="60" /> Mbps、&lt;0 禁用、0 不限制
                </el-form-item>
                <el-form-item :label="$t('server.wlistRemark')" prop="Remark">
                    <el-input v-trim v-model="state.ruleForm.Remark" />
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">{{$t('common.cancel')}}</el-button>
                        <el-button type="primary" @click="handleSave">{{$t('common.confirm')}}</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
    <el-dialog class="options-center" :title="$t('server.wlistNodes')" destroy-on-close v-model="state.showNodes" width="54rem" top="2vh">
        <div>
            <el-transfer class="src-tranfer"
                v-model="state.nodeIds"
                filterable
                :filter-method="srcFilterMethod"
                :data="state.nodes"
                :titles="[$t('server.wlistUnselect'), $t('server.wlistSelected')]"
                :props="{key: 'Id',label: 'Name'}"/>
            <div class="t-c w-100 mgt-1">
                <el-button @click="state.showNodes = false">{{$t('common.cancel')}}</el-button>
                <el-button type="primary" @click="handleNodes">{{$t('common.confirm')}}</el-button>
            </div>
        </div>
    </el-dialog>
</template>

<script>
import { ElMessage } from 'element-plus';
import { computed, inject, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import { wlistAdd } from '@/apis/wlist';
import { getSignInUserIds } from '@/apis/signin';
import moment from 'moment/moment';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();
        const nodes = inject('nodes');
        const nodeJson =[{Id:'*',Name:'*'}].concat(nodes.value).reduce((json,item,index)=>{ json[item.Id] = item.Name; return json; },{});
        const editState = inject('edit');

        const date = new Date();
        const end = new Date(date.getFullYear()+1,date.getMonth(),date.getDate());
        
        const state = reactive({
            show:true,
            
            apply2user:!!editState.value.UserId,
            timeRange:[
                (editState.value.UseTime || moment(date).format('YYYY-MM-DD')).split(' ')[0],
                (editState.value.EndTime || moment(end).format('YYYY-MM-DD')).split(' ')[0],
            ],

            ruleForm:{
                Id:editState.value.Id || 0,
                Type:editState.value.Type || '',
                MachineId:editState.value.MachineId || '',
                UserId:editState.value.UserId || '',
                Name:editState.value.Name || '',
                Remark:editState.value.Remark || '',
                Nodes:editState.value.Nodes || ['*'],
                Bandwidth:editState.value.Bandwidth || 0,
                UseTime:editState.value.UseTime || '',
                EndTime:editState.value.EndTime || '',
            },
            
            rules:{
                UserId: [{ required: true, message: "required", trigger: "blur" }],
                Name: [{ required: true, message: "required", trigger: "blur" }],
                Nodes: [{ required: true, message: "required", trigger: "blur" }],
            },
            nodes:computed(()=>[{Id:'*',Name:'*'}].concat(nodes.value)),
            nodesText:computed(()=>state.ruleForm.Nodes.filter(c=>!!!state.prefix || c.indexOf(state.prefix) < 0).map(c=>nodeJson[c]).join(',')),

            showNodes:false,
            nodeIds: [],
            ports: [],
            machineids:[],
            prefix:editState.value.prefix
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const formatNodes = ()=>{
            const ports = state.ports.replace(/\n/g,',').split(',').map(c=>c.replace(/\s/g,'')).filter(c=>!!c).map(c=>`${state.prefix}${c}`);
            state.ruleForm.Nodes = state.nodeIds.concat(ports);
        }

        const handleShowNodes = ()=>{  
            return false;   
            state.nodeIds = state.ruleForm.Nodes.filter(c=>!!!state.prefix ||c.indexOf(state.prefix) < 0);
            state.showNodes = true;
        }
        const srcFilterMethod = (query, item) => {
            return item.Name.toLowerCase().includes(query.toLowerCase())
        }
        const handleNodes = ()=>{
            state.ports = '';
            formatNodes();
            state.showNodes = false;
        }
        const handlePortChange = ()=>{
            state.nodeIds = [];
            formatNodes();        
        }
        const handleMachineIdChange = ()=>{
            try{
                state.ruleForm.Name = state.machineids.filter(c=>c.MachineId == state.ruleForm.MachineId)[0].MachineName 
            }catch(e){
            }
        }
        const handleMachineIds = (query)=>{
            getSignInUserIds(query).then(data=>{
                data.forEach(c=>{
                    c.MachineId = c.MachineId || '';
                })
                state.machineids = data;
            }).catch(()=>{});
        }

        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));
                if(state.apply2user){
                    json.UserId = state.machineids.filter(c=>c.MachineId == state.ruleForm.MachineId)[0].UserId;
                }
                json.UseTime = `${moment(state.timeRange[0]).format('YYYY-MM-DD')} 00:00:00`;
                json.EndTime = `${moment(state.timeRange[1]).format('YYYY-MM-DD')} 23:59:59`;
                wlistAdd(json).then(()=>{
                    ElMessage.success(t('common.oper'));
                    state.show = false;
                    emit('success');
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }

        onMounted(()=>{
            state.nodeIds = state.ruleForm.Nodes.filter(c=>!!!state.prefix ||c.indexOf(state.prefix) < 0).map(c=>c.replace(state.prefix,''));
            state.ports = state.ruleForm.Nodes.filter(c=>!!!state.prefix ||c.indexOf(state.prefix) >= 0).map(c=>c.replace(state.prefix,'')).join(',');
            handleMachineIds(state.ruleForm.MachineId);
        });

        return {state,handleShowNodes,srcFilterMethod,handleNodes,ruleFormRef,handleSave,handlePortChange,handleMachineIdChange,handleMachineIds}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>