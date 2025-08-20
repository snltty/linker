<template>
   <el-dialog class="options-center" :title="$t('server.wlist')" destroy-on-close v-model="state.show" width="36rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('server.wlistUserId')" prop="UserId">
                    <el-select v-model="state.ruleForm.UserId" filterable remote :loading="state.loading" :remote-method="handleUserIds" @change="handleUserIdChange">
                        <el-option v-for="(item, index) in state.userids" :key="index" :label="item.MachineName" :value="item.UserId">
                        </el-option>
                    </el-select>
                </el-form-item>
                <el-form-item :label="$t('server.wlistName')" prop="Name">
                    <el-input v-trim v-model="state.ruleForm.Name" />
                </el-form-item>
                <el-form-item :label="$t(`server.wlistNodes`)" prop="Nodes">
                    <el-input v-trim type="textarea" :value="state.nodesText" @click="handleShowNodes" readonly resize="none" :rows="4"></el-input>
                </el-form-item>
                <el-form-item v-if="state.prefix" :label="$t(`server.wlistNodes${state.ruleForm.Type}`)" prop="Domain">
                    <el-input v-trim type="textarea" v-model="state.ports" resize="none" :rows="2" @change="handlePortChange"></el-input>
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
export default {
    props: ['modelValue'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();
        const nodes = inject('nodes');
        const nodeJson =[{Id:'*',Name:'*'}].concat(nodes.value).reduce((json,item,index)=>{ json[item.Id] = item.Name; return json; },{});
        const editState = inject('edit');
        const state = reactive({
            show:true,
            
            ruleForm:{
                Id:editState.value.Id || 0,
                Type:editState.value.Type || '',
                UserId:editState.value.UserId || '',
                Name:editState.value.Name || '',
                Remark:editState.value.Remark || '',
                Nodes:editState.value.Nodes || [],
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
            userids:[],
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
            state.nodeIds = state.ruleForm.Nodes.filter(c=>!!!state.prefix ||c.indexOf(state.prefix) < 0);
            state.showNodes = true;
        }
        const srcFilterMethod = (query, item) => {
            return item.Name.toLowerCase().includes(query.toLowerCase())
        }
        const handleNodes = ()=>{
            formatNodes();
            state.showNodes = false;
        }
        const handlePortChange = ()=>{
            formatNodes();        
        }
        const handleUserIdChange = ()=>{
            try{
                state.ruleForm.Name = state.userids.filter(c=>c.UserId == state.ruleForm.UserId)[0].MachineName 
            }catch(e){
            }
        }
        const handleUserIds = (query)=>{
            getSignInUserIds(query).then(data=>{
                data.forEach(c=>{
                    c.UserId = c.UserId || '';
                })
                state.userids = data;
            }).catch(()=>{});
        }

        const ruleFormRef = ref(null);
        const handleSave = ()=>{       
            ruleFormRef.value.validate((valid) => {
                if (!valid) return;

                const json = JSON.parse(JSON.stringify(state.ruleForm));
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
            handleUserIds(state.ruleForm.UserId);
        });

        return {state,handleShowNodes,srcFilterMethod,handleNodes,ruleFormRef,handleSave,handlePortChange,handleUserIdChange,handleUserIds}
    }
}
</script>
<style lang="stylus" scoped>
.el-form-item{margin-bottom:1rem}
.el-input-number--small{width:10rem !important}
</style>