<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="配置本组的网络" top="1vh" width="500">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="60">
                <el-form-item label="网卡名" prop="Name">
                    <el-row class="w-100">
                        <el-col :span="10">
                            <el-input v-trim v-model="state.ruleForm.Name"/>
                        </el-col>
                        <el-col :span="14"></el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="网络号" prop="IP">
                    <el-row class="w-100">
                        <el-col :span="13">
                            <el-input v-trim v-model="state.ruleForm.IP" @change="handlePrefixLengthChange" />
                        </el-col>
                        <el-col :span="1" class="t-c">/</el-col>
                        <el-col :span="3">
                            <el-input v-trim @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" />
                        </el-col>
                        <el-col :span="1" class="t-c"></el-col>
                        <el-col :span="6">
                            <el-button @click="handleClear"><el-icon><Refresh /></el-icon></el-button>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" prop="IP1">
                    <div class="w-100">
                        <el-descriptions :column="2" size="small" border title="">
                            <el-descriptions-item label="网络号">{{ state.values.Network }}</el-descriptions-item>
                            <el-descriptions-item label="网关">{{ state.values.Gateway }}</el-descriptions-item>
                            <el-descriptions-item label="开始IP">{{ state.values.Start }}</el-descriptions-item>
                            <el-descriptions-item label="结束IP">{{ state.values.End }}</el-descriptions-item>
                            <el-descriptions-item label="广播号">{{ state.values.Broadcast }}</el-descriptions-item>
                            <el-descriptions-item label="IP数量">{{ state.values.Count }}</el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-form-item>
                <el-form-item label="子网" prop="Subs">
                    <div class="subs">
                        <template v-for="(item,index) in state.ruleForm.Subs">
                            <el-row class="w-100">
                                <el-col :span="4" class="pdr-10">
                                    <el-input v-trim v-model="item.Name"/>
                                </el-col>
                                <el-col :span="7">
                                    <el-input v-trim v-model="item.IP"/>
                                </el-col>
                                <el-col :span="1" class="t-c">/</el-col>
                                <el-col :span="3">
                                    <el-input v-trim v-model="item.PrefixLength"/>
                                </el-col>
                                <el-col :span="9" class="t-r">
                                    <el-button type="danger"><el-icon><Delete></Delete></el-icon></el-button>
                                    <el-button type="info"><el-icon><Edit></Edit></el-icon></el-button>
                                    <el-button type="primary"><el-icon><Plus></Plus></el-icon></el-button>
                                </el-col>
                            </el-row>
                        </template>
                    </div>
                </el-form-item>
                <el-form-item label="" prop="alert"></el-form-item>
                <AccessShow value="Lease">
                    <el-form-item label="" prop="Btns">
                        <div>
                            <el-button @click="state.show = false">取消</el-button>
                            <el-button type="primary" @click="handleSave">确认</el-button>
                        </div>
                    </el-form-item>
                </AccessShow>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import {getNetwork,addNetwork,calcNetwork } from '@/apis/tuntap';
import { ElMessage } from 'element-plus';
import { onMounted, reactive, ref, watch } from 'vue';
import { Delete, Plus,Refresh,Edit } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus,Refresh,Edit},
    setup(props, { emit }) {

        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                Name:'',
                IP:'0.0.0.0',
                PrefixLength:24,
                Subs:[]
            },
            rules: {
                Name: {
                    type: 'string',
                    pattern: /^$|^[A-Za-z][A-Za-z0-9]{0,31}$/,
                    message:'请输入正确的网卡名',
                    transform(value) {
                        return value.trim();
                    },
                }
            },
            values:{}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _calcNetwork = ()=>{
            calcNetwork(state.ruleForm).then((res)=>{
                state.values = res;
            });
        }
        const _getNetwork = ()=>{
            getNetwork().then((res)=>{
                state.ruleForm.Name = res.Name;
                state.ruleForm.IP = res.IP;
                state.ruleForm.PrefixLength = res.PrefixLength;
                if(res.Subs.length == 0){
                    res.Subs = [{Name:'子网1',IP:'0.0.0.0',PrefixLength:24}];
                }
                state.ruleForm.Subs = res.Subs;
                _calcNetwork();
            });
        }
        const handlePrefixLengthChange = ()=>{
            var value = +state.ruleForm.PrefixLength;
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
            _calcNetwork();
        }
        const handleSave = () => {
            addNetwork(state.ruleForm).then(()=>{
                ElMessage.success('已操作');
                state.show = false;
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            })
        }
        const handleClear = ()=>{
            addNetwork({Name:'',IP:'0.0.0.0',PrefixLength:24,Subs:[]}).then(()=>{
                ElMessage.success('已操作');
                _getNetwork();
            }).catch((err)=>{
                console.log(err);
                ElMessage.error('操作失败');
            });
        }

        onMounted(()=>{
            _getNetwork();
        })

        return {
           state,ruleFormRef, handleSave,handlePrefixLengthChange,handleClear
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.el-button+.el-button{
    margin-left: .4rem;
}
</style>