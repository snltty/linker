<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="配置本组的网络" top="1vh" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="100">
                <el-form-item prop="gateway">
                    <p>网络租期30天、IP租期7天</p>
                </el-form-item>
                <el-form-item label="网卡名" prop="Name">
                    <el-input v-trim v-model="state.ruleForm.Name" style="width:14rem"/>
                </el-form-item>
                <el-form-item label="网络前缀" prop="IP">
                    <el-input v-trim v-model="state.ruleForm.IP" style="width:14rem" @change="handlePrefixLengthChange" />
                    <span>/</span>
                    <el-input v-trim @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
                    <span style="width: 1rem;"></span>
                    <el-button @click="handleClear">重置</el-button>
                </el-form-item>
                <el-form-item label="" prop="IP1">
                    <div class="calc">
                        <p><span class="label">网络号</span><span class="value">{{ state.values.Network }}</span></p>
                        <p><span class="label">网关</span><span class="value">{{ state.values.Gateway }}</span></p>
                        <p><span class="label">开始IP</span><span class="value">{{ state.values.Start }}</span></p>
                        <p><span class="label">结束IP</span><span class="value">{{ state.values.End }}</span></p>
                        <p><span class="label">广播号</span><span class="value">{{ state.values.Broadcast }}</span></p>
                        <p><span class="label">IP数量</span><span class="value">{{ state.values.Count }}</span></p>
                    </div>
                </el-form-item>
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
import { Delete, Plus } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus},
    setup(props, { emit }) {

        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                Name:'',
                IP:'0.0.0.0',
                PrefixLength:24
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
            addNetwork({Name:'',IP:'0.0.0.0',PrefixLength:24}).then(()=>{
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
.calc{
    span{
        display: inline-block;
        &.label{width:6rem;}
    }
}

</style>