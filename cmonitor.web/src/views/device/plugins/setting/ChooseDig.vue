<template>
    <el-dialog class="options-center" title="设置" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <div class="setting-wrap">
            <el-form ref="ruleFormRef" :model="state.form" :rules="state.rules" label-width="100px">
                <el-form-item label="报告延迟(ms)" prop="ReportDelay">
                    <el-input-number size="large" v-model="state.form.ReportDelay" :min="17" :max="1000" controls-position="right" />
                </el-form-item>
                <el-form-item label="截屏延迟(ms)" prop="ScreenDelay">
                    <el-input-number size="large" v-model="state.form.ScreenDelay" :min="17" :max="1000" controls-position="right" />
                </el-form-item>
                <el-form-item label="截屏缩放" prop="ScreenScale">
                    <el-input-number size="large" v-model="state.form.ScreenScale" :min="0.1" :max="1" :step="0.1" controls-position="right" />
                </el-form-item>
                <!-- <el-form-item label="保存配置" prop="SaveSetting">
                    <el-checkbox v-model="state.form.SaveSetting">保存限制配置</el-checkbox>
                </el-form-item>
                <el-form-item label="黑屏唤醒" prop="WakeUp">
                    <el-checkbox v-model="state.form.WakeUp">黑屏时唤醒</el-checkbox>
                </el-form-item>
                <el-form-item label="声音峰值" prop="VolumeMasterPeak">
                    <el-checkbox v-model="state.form.VolumeMasterPeak">报告声音峰值</el-checkbox>
                </el-form-item> -->
            </el-form>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { onMounted, watch } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import { getSetting, saveSetting } from '../../../../apis/setting'
import { ElMessage } from 'element-plus';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { CheckBoxWrap },
    setup(props, { emit }) {
        const state = reactive({
            show: props.modelValue,
            loading: false,
            rules: [],
            form: {
                ReportDelay: 0,
                ScreenDelay: 0,
                ScreenScale: 0,
                SaveSetting: true,
                WakeUp: true,
                VolumeMasterPeak: true,
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const loadData = () => {
            getSetting().then((res) => {
                state.form.ReportDelay = res.ReportDelay;
                state.form.ScreenDelay = res.ScreenDelay;
                state.form.ScreenScale = res.ScreenScale;
                state.form.SaveSetting = res.SaveSetting;
                state.form.WakeUp = res.WakeUp;
                state.form.VolumeMasterPeak = res.VolumeMasterPeak;
            }).catch(() => {
            });
        }

        const handleCancel = () => {
            state.show = false;
        }
        const devices = ref(null);
        const handleSubmit = () => {
            state.loading = true;
            saveSetting(state.form).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.success('操作成功！');
                    state.show = false;
                } else {
                    ElMessage.error('操作失败！');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败!');
            });
        }

        onMounted(() => {
            loadData();
        });

        return {
            state, devices, handleCancel, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
.setting-wrap {
    position: relative;
    padding: 2rem 5rem;
}
</style>