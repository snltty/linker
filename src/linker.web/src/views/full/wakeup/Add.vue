<template>
    <el-dialog class="options-center" :title="$t('wakeup.rule')" destroy-on-close v-model="state.show" width="50rem" top="2vh">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm.Data" :rules="state.rules" label-width="auto">
                <el-form-item :label="$t('wakeup.name')" prop="Name">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-input v-model="state.ruleForm.Data.Name" maxlength="32" show-word-limit />
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('wakeup.type')" prop="Type">
                    <el-row class="w-100">
                        <el-col :span="12">
                            <el-select v-model="state.ruleForm.Data.Type" >
                                <el-option :value="item.value" :label="item.label" v-for="(item,index) in state.types"></el-option>
                            </el-select>
                        </el-col>
                        <el-col :span="12"></el-col>
                    </el-row>
                </el-form-item>
                <template  v-if="state.ruleForm.Data.Type == 1">
                    <el-form-item :label="$t('wakeup.valueMac')" prop="mac">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-input v-model="state.ruleForm.Data.mac" />
                            </el-col>
                            <el-col :span="12"></el-col>
                        </el-row>
                    </el-form-item>
                </template>
                <template  v-if="state.ruleForm.Data.Type == 2">
                    <el-form-item :label="$t('wakeup.valueCom')" prop="com">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-select v-model="state.ruleForm.Data.com" >
                                    <el-option :value="item" :label="item" v-for="(item,index) in state.coms"></el-option>
                                </el-select>
                            </el-col>
                            <el-col :span="12"></el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item label=" ">{{ $t('wakeup.valueComText') }}</el-form-item>
                    <el-form-item :label="$t('wakeup.contentOpen')" prop="open">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-input v-model="state.ruleForm.Data.open" />
                            </el-col>
                            <el-col :span="12">{{ $t('wakeup.contentOpenText') }}</el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item :label="$t('wakeup.contentClose')" prop="close">
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-input v-model="state.ruleForm.Data.close" />
                            </el-col>
                            <el-col :span="12">{{ $t('wakeup.contentCloseText') }}</el-col>
                        </el-row>
                    </el-form-item>
                </template>
                <el-form-item></el-form-item>
                <el-form-item :label="$t('wakeup.remark')" prop="Remark">
                    <el-input v-model="state.ruleForm.Data.Remark" maxlength="64" show-word-limit />
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
import { addWakeup, getWakeupComs } from '@/apis/wakeup';
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
                    Content:add.value.Data.Content,
                    Remark:add.value.Data.Remark,

                    open:(add.value.Data.Type == 2 ? add.value.Data.Content.split('|') : [])[0] || '0xA0,0x01,0x01,0xA2',
                    close:(add.value.Data.Type == 2 ? add.value.Data.Content.split('|') : [])[1] || '0xA0,0x01,0x00,0xA1',
                    mac:add.value.Data.Type == 1 ? add.value.Data.Value : '',
                    com:add.value.Data.Type == 2 ? add.value.Data.Value : ''
                }
            },
            rules:{
                Name: [{ required: true, message: "required", trigger: "blur" }],
                mac: [{ validator:(rule,value,callback)=>{
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
                open: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 2){
                        if (rule.pattern.test(value)) {
                            callback();
                        } else {
                            callback(new Error('failed'));
                        }
                    }else{
                        callback();
                    }
                },pattern:/^(0x[0-9A-Fa-f]{1,2})(,0x[0-9A-Fa-f]{1,2})*$/, trigger: "blur" }],
                close: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 2){
                        if (rule.pattern.test(value)) {
                            callback();
                        } else {
                            callback(new Error('failed'));
                        }
                    }else{
                        callback();
                    }
                },pattern:/^(0x[0-9A-Fa-f]{1,2})(,0x[0-9A-Fa-f]{1,2})*$/, trigger: "blur" }],
                com: [{ validator:(rule,value,callback)=>{
                    if(state.ruleForm.Data.Type == 2 && !value){
                        callback(new Error('failed'));
                    }else{
                        callback();
                    }
                }, trigger: "blur" }],
            },
            coms:[],
            types: [
                {label:t('wakeup.typeWol'),value:1},
                {label:t('wakeup.typeSwitch'),value:2},
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
                const json = {
                    MachineId:state.ruleForm.MachineId,
                    Data:{
                        Id:state.ruleForm.Data.Id,
                        Name:state.ruleForm.Data.Name.replace(/^\s|\s$/g,''),
                        Type:state.ruleForm.Data.Type,
                        Value:state.ruleForm.Data.Type == 1 ? state.ruleForm.Data.mac.replace(/^\s|\s$/g,'') : state.ruleForm.Data.com.replace(/^\s|\s$/g,''),
                        Content:state.ruleForm.Data.Type == 2 ? state.ruleForm.Data.open.replace(/^\s|\s$/g,'') + '|' + state.ruleForm.Data.close.replace(/^\s|\s$/g,'') : '',
                        Remark:state.ruleForm.Data.Remark.replace(/^\s|\s$/g,'')
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

        onMounted(()=>{
            getWakeupComs().then((res)=>{
                state.coms = res;
                if(!state.ruleForm.Data.com && res.length > 0){
                    state.ruleForm.Data.com = res[0];
                }
            }).catch(()=>{});
        })
        return {state,ruleFormRef,handleSave}
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