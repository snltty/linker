<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" title="组网设置" top="1vh" width="270">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="0">
                <el-form-item prop="gateway" style="margin-bottom:0">
                    赐予此设备IP，其它设备可通过此IP访问
                </el-form-item>
                <el-form-item label="" prop="IP" style="margin-bottom:0">
                    <el-input v-model="state.ruleForm.IP" style="width:14rem" />
                    <span>/</span>
                    <el-input @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
                </el-form-item>
                <el-form-item label="" prop="ShowDelay">
                    <el-checkbox v-model="state.ruleForm.ShowDelay" label="显示延迟" size="large" />
                    <el-checkbox v-model="state.ruleForm.AutoConnect" label="自动连接？" size="large" />
                </el-form-item>
                <el-form-item label="" prop="Btns">
                    <div class="t-c w-100">
                        <el-button @click="state.show = false">取消</el-button>
                        <el-button type="primary" @click="handleSave">确认</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </el-dialog>
</template>
<script>
import {updateTuntap } from '@/apis/tuntap';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import { useTuntap } from '../full/devices/tuntap';
import { Delete, Plus } from '@element-plus/icons-vue'
export default {
    props: ['modelValue'],
    emits: ['change','update:modelValue'],
    components: {Delete,Plus},
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const tuntap = useTuntap();
        const ruleFormRef = ref(null);
        const state = reactive({
            show: true,
            bufferSize:globalData.value.bufferSize,
            ruleForm: {
                IP: tuntap.value.current.IP,
                LanIPs: tuntap.value.current.LanIPs.slice(0),
                Masks: tuntap.value.current.Masks.slice(0),
                PrefixLength:tuntap.value.current.PrefixLength || 24,
                Gateway: tuntap.value.current.Gateway,
                ShowDelay: tuntap.value.current.ShowDelay,
                AutoConnect: tuntap.value.current.AutoConnect,
                Upgrade: tuntap.value.current.Upgrade,

                Forwards:tuntap.value.current.Forwards.length == 0 ? [
                    {ListenAddr:'0.0.0.0',ListenPort:0,ConnectAddr:'0.0.0.0',ConnectPort:0}
                ] : tuntap.value.current.Forwards.slice(0)
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

        const handlePrefixLengthChange = ()=>{
            var value = +state.ruleForm.PrefixLength;
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.PrefixLength = value;
        }
        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tuntap.value.current));
            json.IP = state.ruleForm.IP || '0.0.0.0';
            const {lanips,masks} = state.ruleForm.LanIPs.reduce((json,ip,index)=>{
                if(ip && state.ruleForm.Masks[index]){
                    json.lanips.push(ip);
                    json.masks.push(state.ruleForm.Masks[index]);
                }
                return json;
            },{lanips:[],masks:[]});
            json.LanIPs = lanips;
            json.Masks = masks;
            json.PrefixLength = +state.ruleForm.PrefixLength;
            json.Gateway = state.ruleForm.Gateway;
            json.ShowDelay = state.ruleForm.ShowDelay;
            json.AutoConnect = state.ruleForm.AutoConnect;
            json.Upgrade = state.ruleForm.Upgrade;
            json.Forwards = state.ruleForm.Forwards;
            json.Forwards.forEach(c=>{
                c.ListenPort=+c.ListenPort;
                c.ConnectPort=+c.ConnectPort;
            });
            updateTuntap(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }
        return {
           state, ruleFormRef,handlePrefixLengthChange, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}

.upgrade-wrap{
    border:1px solid #ddd;
    margin-bottom:2rem
    padding:0 0 1rem 0;
}
</style>