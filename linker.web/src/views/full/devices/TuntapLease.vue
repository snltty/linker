<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="配置本组的网络" top="1vh" width="400">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="100">
                <el-form-item prop="gateway">
                    <p>网络租期30天、IP租期7天</p>
                </el-form-item>
                <el-form-item label="网络和掩码" prop="IP">
                    <el-input v-model="state.ruleForm.IP" style="width:14rem" />
                    <span>/</span>
                    <el-input @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
                    <span style="width: 1rem;"></span>
                    <el-button @click="handleClear">清除</el-button>
                </el-form-item>
                <el-form-item label="" prop="Btns" v-if="hasLease">
                    <div>
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import {getNetwork,addNetwork } from '@/apis/tuntap';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { Delete, Plus } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const hasLease = computed(()=>globalData.value.hasAccess('Lease')); 

        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            ruleForm: {
                IP:'0.0.0.0',
                PrefixLength:24
            },
            rules: {}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const _getNetwork = ()=>{
            getNetwork().then((res)=>{
                state.ruleForm.IP = res.IP;
                state.ruleForm.PrefixLength = res.PrefixLength;
            });
        }
        const handlePrefixLengthChange = ()=>{
            var value = +state.ruleForm.PrefixLength;
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
        }
        const handleSave = () => {
            addNetwork(state.ruleForm).then(()=>{
                ElMessage.success('已操作');
                state.show = false;
            }).catch(()=>{
                ElMessage.error('操作失败');
            })
        }
        const handleClear = ()=>{
            addNetwork({IP:'0.0.0.0',PrefixLength:24}).then(()=>{
                ElMessage.success('已操作');
                _getNetwork();
            }).catch(()=>{
                ElMessage.error('操作失败');
            });
        }

        onMounted(()=>{
            _getNetwork();
        })

        return {
           state,hasLease, ruleFormRef, handleSave,handlePrefixLengthChange,handleClear
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
</style>