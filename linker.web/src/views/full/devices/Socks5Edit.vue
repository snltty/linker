<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" :title="`设置[${state.machineName}]代理`" top="1vh" width="600">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="140">
                <el-form-item prop="gateway" style="margin-bottom:0">
                    配置代理，通过代理访问其它设备
                </el-form-item>
                <el-form-item label="代理端口" prop="Port">
                    <el-input v-model="state.ruleForm.Port" style="width:14rem" />
                </el-form-item>
                <div class="upgrade-wrap">
                    <el-form-item label="此设备局域网IP" prop="LanIP" class="lan-item">
                        <template v-for="(item, index) in state.ruleForm.Lans" :key="index">
                            <div class="flex" style="margin-bottom:.6rem">
                                <div class="flex-1">
                                    <el-input v-model="item.IP" style="width:14rem" />
                                    <span>/</span>
                                    <el-input @change="handleMaskChange(index)" v-model="item.PrefixLength" style="width:4rem" />
                                </div>
                                <div class="pdl-10">
                                    <el-checkbox v-model="item.Disabled" label="禁用记录" size="large" />
                                </div>
                                <div class="pdl-10">
                                    <el-button type="danger" @click="handleDel(index)"><el-icon><Delete /></el-icon></el-button>
                                    <el-button type="primary" @click="handleAdd(index)"><el-icon><Plus /></el-icon></el-button>
                                </div>
                            </div>
                        </template>
                    </el-form-item>
                </div>
                <el-form-item label="" prop="Btns">
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
import {updateSocks5 } from '@/apis/socks5';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import { useSocks5 } from './socks5';
import { Delete, Plus } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const socks5 = useSocks5();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            machineName:socks5.value.current.device.MachineName,
            bufferSize:globalData.value.bufferSize,
            ruleForm: {
                Port: socks5.value.current.Port,
                Lans: socks5.value.current.Lans.slice(0)
            },
            rules: {}
        });
        if (state.ruleForm.Lans.length == 0) {
            state.ruleForm.Lans.push({IP:'0.0.0.0',PrefixLength:24});
        }
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleMaskChange = (index)=>{
            var value = +state.ruleForm.Lans[index].PrefixLength;
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.Lans[index].PrefixLength = value;
        }
        const handleDel = (index) => {
            state.ruleForm.Lans.splice(index, 1);
            if (state.ruleForm.Lans.length == 0){
                handleAdd(0);
            }
        }
        const handleAdd = (index) => {
            state.ruleForm.Lans.splice(index + 1, 0, {IP:'0.0.0.0',PrefixLength:24});
        }
        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(socks5.value.current));
            json.Port = +(state.ruleForm.Port || '1805');
            json.Lans = state.ruleForm.Lans.map(c=>{ c.PrefixLength=+c.PrefixLength;return c; });
            updateSocks5(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }

        return {
           state, ruleFormRef,handleMaskChange,  handleDel, handleAdd, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
.upgrade-wrap{
    border:1px solid #ddd;
    margin-bottom:2rem
    padding:1rem 0 1rem 0;
}
.lan-item{
    margin-bottom:0;
}
</style>