<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="组网设置" top="1vh" width="700">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="140">
                <el-form-item prop="gateway" style="margin-bottom:0">
                    赐予此设备IP，其它可以通过这个IP访问
                </el-form-item>
                <el-form-item label="此设备的虚拟网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" style="width:12rem" /> / <el-input @change="handlePrefixLengthChange" v-model="state.ruleForm.PrefixLength" style="width:4rem" />
                </el-form-item>
                <el-form-item prop="upgrade" style="margin-bottom:0">
                    <el-checkbox v-model="state.ruleForm.Upgrade" label="我很懂，我要使用高级功能(点对网和网对网)" size="large" />
                </el-form-item>
                <div class="upgrade-wrap" v-if="state.ruleForm.Upgrade">
                    <el-form-item prop="gateway" style="margin-bottom:0">
                        <el-checkbox v-model="state.ruleForm.Gateway" label="此设备在路由器(网对网，将对方的 局域网IP 转发到对方)" size="large" />
                    </el-form-item>
                    <el-form-item prop="gateway" style="margin-bottom:0">
                        <span class="yellow">此设备可以用NAT转发，那填写局域网IP，其它交给NAT(linux、macos、win10+)</span>
                    </el-form-item>
                    <el-form-item label="此设备局域网IP" prop="LanIP">
                        <template v-for="(item, index) in state.ruleForm.LanIPs" :key="index">
                            <div class="flex" style="margin-bottom:.6rem">
                                <div class="flex-1">
                                    <el-input v-model="state.ruleForm.LanIPs[index]" style="width:12rem" /> / <el-input @change="handleMaskChange(index)" v-model="state.ruleForm.Masks[index]" style="width:4rem" />
                                </div>
                                <div class="pdl-10">
                                    <el-button type="danger" @click="handleDel(index)"><el-icon><Delete /></el-icon></el-button>
                                    <el-button type="primary" @click="handleAdd(index)"><el-icon><Plus /></el-icon></el-button>
                                </div>
                            </div>
                        </template>
                    </el-form-item>
                    <el-form-item prop="gateway" style="margin-bottom:0">
                        <span class="yellow">此设备不能用NAT转发，那可以使用系统端口转发实现类似的效果(仅windows)</span>
                    </el-form-item>
                    <el-form-item label="端口转发" prop="foreards">
                        <template v-for="(item, index) in state.ruleForm.Forwards" :key="index">
                            <div class="flex" style="margin-bottom:.6rem">
                                <div class="flex-1">
                                    <el-input v-model="item.ListenAddr" style="width:7rem" readonly /> : <el-input @change="handleForwardChange(index)" v-model="item.ListenPort" style="width:6rem" />
                                     -> <el-input v-model="item.ConnectAddr" style="width:12rem" /> : <el-input @change="handleForwardChange(index)" v-model="item.ConnectPort" style="width:6rem" />
                                </div>
                                <div class="pdl-10">
                                    
                                    <el-button type="danger" @click="handleDelForward(index)"><el-icon><Delete /></el-icon></el-button>
                                    <el-button type="primary" @click="handleAddForward(index)"><el-icon><Plus /></el-icon></el-button>
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
import {updateTuntap } from '@/apis/tuntap';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive, ref, watch } from 'vue';
import { useTuntap } from './tuntap';
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
                Upgrade: tuntap.value.current.Upgrade,

                Forwards:tuntap.value.current.Forwards.slice(0) || [
                    {ListenAddr:'0.0.0.0',ListenPort:0,ConnectAddr:'0.0.0.0',ConnectPort:0}
                ]
            },
            rules: {}
        });
        if (state.ruleForm.LanIPs.length == 0) {
            state.ruleForm.LanIPs.push('');
            state.ruleForm.Masks.push(24);
        }
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
        const handleMaskChange = (index)=>{
            var value = +state.ruleForm.Masks[index];
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.Masks[index] = value;
        }
        const handleDel = (index) => {
            state.ruleForm.LanIPs.splice(index, 1);
            state.ruleForm.Masks.splice(index, 1);
            if (state.ruleForm.LanIPs.length == 0){
                handleAdd(0);
            }
        }
        const handleAdd = (index) => {
            state.ruleForm.LanIPs.splice(index + 1, 0, '');
            state.ruleForm.Masks.splice(index + 1, 0, 24);
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

        const handleDelForward = (index) => {
            state.ruleForm.Forwards.splice(index, 1);
            if (state.ruleForm.Forwards.length == 0) {
                handleAddForward(0);
            }
        }
        const handleAddForward = (index) => {
            state.ruleForm.Forwards.splice(index + 1, 0, {ListenAddr:'0.0.0.0',ListenPort:0,ConnectAddr:'0.0.0.0',ConnectPort:0});
        }
        const handleForwardChange = ()=>{

        }

        return {
           state, ruleFormRef,handlePrefixLengthChange,handleMaskChange,  handleDel, handleAdd, handleSave,
           handleForwardChange,handleDelForward,handleAddForward
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}

.upgrade-wrap{
    border:1px solid #ddd;
    margin-bottom:2rem
    padding:1rem 0;
}
</style>