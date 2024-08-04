<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="设置虚拟网卡IP" width="420">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="80">
                <!-- <el-form-item label="缓冲区" prop="BufferSize">
                    <el-select v-model="state.ruleForm.BufferSize" placeholder="Select" style="width:12rem">
                        <el-option v-for="(item,index) in state.bufferSize" :key="index" :label="item" :value="index"/>
                    </el-select>
                </el-form-item> -->
                <el-form-item label="">
                    <div>局域网IP选填，不懂是啥的时候，不要填</div>
                </el-form-item>
                <el-form-item label="虚拟网卡IP" prop="IP">
                    <el-input v-model="state.ruleForm.IP" style="width:12rem" /> / 24
                </el-form-item>
                <el-form-item label="局域网IP" prop="LanIP">
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
                BufferSize: tuntap.value.current.BufferSize
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

        const handleMaskChange = (index)=>{
            var value = +state.ruleForm.Masks[index];
            if(value>32 || value<16 || isNaN(value)){
                value = 24;
            }
            state.ruleForm.Masks[index] = value;
        }
        const handleDel = (index) => {
            if (state.ruleForm.LanIPs.length == 1) return;
            state.ruleForm.LanIPs.splice(index, 1);
            state.ruleForm.Masks.splice(index, 1);
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
            json.BufferSize = state.ruleForm.BufferSize;
            updateTuntap(json).then(() => {
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
</style>