<template>
    <el-dialog class="options-center" :title="$t('wakeup.rule')" destroy-on-close v-model="state.show" width="50rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm.Data" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('wakeup.name')" prop="Name">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-trim v-model="state.ruleForm.Data.Name" maxlength="32" show-word-limit />
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('wakeup.type')" prop="Type">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-select v-model="state.ruleForm.Data.Type" @change="handleTypeChange" >
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.types"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <template  v-if="state.ruleForm.Data.Type == 1">
                    <el-form-item :label="$t('wakeup.valueMac')" prop="value1">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-input v-trim v-model="state.ruleForm.Data.value1" />
                            </el-col>
                            <el-col :span="12">
                                <el-form-item :label="$t('wakeup.addr')" prop="addr">
                                    <el-input v-trim v-model="state.ruleForm.Data.addr" />
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </el-form-item>
                </template>
                <template  v-if="state.ruleForm.Data.Type == 2">
                    <el-form-item :label="$t('wakeup.valueCom')" prop="value2">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-select v-model="state.ruleForm.Data.value2" >
                                    <el-option :value="item" :label="item" v-for="(item,index) in state.coms"></el-option>
                                </el-select>
                            </el-col>
                            <el-col :span="12">
                                <el-form-item :label="$t('wakeup.road')" prop="road">
                                    <el-input v-trim v-model="state.ruleForm.Data.road" />
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item label=" ">{{ $t('wakeup.valueComText') }}</el-form-item>
                </template>
                <template  v-if="state.ruleForm.Data.Type == 4">
                    <el-form-item :label="$t('wakeup.valueHid')" prop="value4">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-select v-model="state.ruleForm.Data.value4" filterable >
                                    <el-option :value="item" :label="item" v-for="(item,index) in state.hids"></el-option>
                                </el-select>
                            </el-col>
                            <el-col :span="12">
                                <el-form-item :label="$t('wakeup.road')" prop="road">
                                    <el-input v-trim v-model="state.ruleForm.Data.road" />
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item label=" ">{{ $t('wakeup.valueHidText') }}</el-form-item>
                </template>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('wakeup.remark')" prop="Remark">
                    <el-input v-trim v-model="state.ruleForm.Data.Remark" maxlength="64" show-word-limit />
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
import { inject, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n';
import { addWakeup, getWakeupComs, getWakeupHids } from '@/apis/wakeup';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue','success'],
    setup(props,{emit}) {
        const {t} = useI18n();

        const add = inject('add');
        const state = reactive({
            show:true,
            loading:false,

            ruleForm:{
                MachineId:add.value.MachineId,
                Data:{
                    Id:add.value.Data.Id,
                    Type:add.value.Data.Type,
                    Name:add.value.Data.Name,
                    Value:add.value.Data.Value,
                    Remark:add.value.Data.Remark,
                    value1:add.value.Data.Type == 1 ? add.value.Data.Value : '',
                    value2:add.value.Data.Type == 2 ? add.value.Data.Value : '',
                    value4:add.value.Data.Type == 4 ? add.value.Data.Value : '',
                    road:  add.value.Data.Content || '1',
                    addr: add.value.Data.Content || '255.255.255.255',
                }
            },
            rules:{
                Name: [{ required: true, message: "required", trigger: "blur" }],
                value1: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 1){
                        if (rule.pattern.test(value)) {
                            callback();
                        } else {
                            callback(new Error('failed'));
                        }
                    }else{
                        callback();
                    }
                },pattern:/^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/, trigger: "blur" }],
                value2: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 2 && !value){
                        callback(new Error('failed'));
                    }else{
                        callback();
                    }
                }, trigger: "blur" }],
                value4: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 4 && !value){
                        callback(new Error('failed'));
                    }else{
                        callback();
                    }
                }, trigger: "blur" }],
                road: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 2 || state.ruleForm.Data.Type ==4){
                        if (rule.pattern.test(value)) {
                            callback();
                        } else {
                            callback(new Error('failed'));
                        }
                    }else{
                        callback();
                    }
                },pattern:/^[0-9]{1,}$/, trigger: "blur" }],

            },
            coms:[],
            hids:[],
            types: [
                {label:t('wakeup.typeWol'),value:1},
                {label:t('wakeup.typeCom'),value:2},
                {label:t('wakeup.typeHid'),value:4},
            ]
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
                const json = {
                    MachineId:state.ruleForm.MachineId,
                    Data:{
                        Id:state.ruleForm.Data.Id,
                        Name:state.ruleForm.Data.Name,
                        Type:state.ruleForm.Data.Type,
                        Value:state.ruleForm.Data[`value${state.ruleForm.Data.Type}`]|| '',
                        Remark:state.ruleForm.Data.Remark || '',
                        Content:state.ruleForm.Data.Type == 2 || state.ruleForm.Data.Type == 4 
                        ? state.ruleForm.Data.road : state.ruleForm.Data.addr
                    }
                };
                addWakeup(json).then(()=>{
                    ElMessage.success(t('common.oper'));
                    state.show = false;
                    emit('success');
                }).catch(()=>{
                    ElMessage.error(t('common.operFail'));
                });
            });
        }

        const handleTypeChange = ()=>{
            state.ruleForm.Data.road = {1:'',2:'1',4:'1'}[state.ruleForm.Data.Type];
        }

        onMounted(()=>{
            getWakeupComs(state.ruleForm.MachineId).then((res)=>{
                state.coms = res;
                if(!state.ruleForm.Data.value2 && res.length > 0){
                    state.ruleForm.Data.value2 = res[0];
                }
            }).catch(()=>{});
            getWakeupHids (state.ruleForm.MachineId).then((res)=>{
                state.hids = res;
                if(!state.ruleForm.Data.value4 && res.length > 0){
                    state.ruleForm.Data.value4 = res[0];
                }
            }).catch(()=>{});
        })
        return {state,ruleFormRef,handleSave,handleTypeChange}
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