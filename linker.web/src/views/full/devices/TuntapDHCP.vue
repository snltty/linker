<template>
     <el-dialog v-model="state.show" :close-on-click-modal="false" append-to=".app-wrap" title="配置本组的网络" top="1vh" width="700">
        <div>
            <el-form ref="ruleFormRef" :model="state.ruleForm" :rules="state.rules" label-width="140">
                <el-form-item prop="gateway" style="margin-bottom:0">
                    赐予此设备IP，其它设备可通过此IP访问
                </el-form-item>
                <el-form-item label="此设备的虚拟网卡IP" prop="IP">

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
            ruleForm: {
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


        const handleSave = () => {
            const json = JSON.parse(JSON.stringify(tuntap.value.current));
            updateTuntap(json).then(() => {
                state.show = false;
                ElMessage.success('已操作！');
                emit('change')
            }).catch(() => {
                ElMessage.error('操作失败！');
            });
        }

        return {
           state, ruleFormRef, handleSave
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch.is-disabled{opacity :1;}
</style>