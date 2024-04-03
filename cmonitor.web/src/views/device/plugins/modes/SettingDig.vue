<template>
    <div class="modes-setting-wrap">
        <el-dialog class="options options-center" title="模式设置" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
            <div class="modes-wrap flex flex-column flex-nowrap">
                <div class="head t-c">
                    <el-select size="small" v-model="state.mode" placeholder="Select" style="width: 80px;margin-right:.6rem">
                        <el-option v-for="item in state.items" :key="item.Name" :label="item.Name" :value="item.Name" />
                    </el-select>
                    <el-button size="small" @click="handleAdd">增加</el-button>
                    <el-button size="small" @click="handleEdit">编辑</el-button>
                    <el-popconfirm title="删除不可逆，确认吗?" @confirm="handleDelete">
                        <template #reference>
                            <el-button size="small" type="danger">删除</el-button>
                        </template>
                    </el-popconfirm>

                    <el-button size="small" type="primary" @click="handleSave">保存</el-button>
                </div>
                <div class="plugins flex-1">
                    <el-tabs type="border-card" class="absolute flex flex-column flxex-nowrap">
                        <template v-for="(item,index) in commandModules" :key="index">
                            <el-tab-pane :label="item.label" class="absolute">
                                <component :ref="`mode-${item.label}`" :is="item"></component>
                            </el-tab-pane>

                        </template>
                    </el-tabs>
                </div>
            </div>
            <template #footer>
                <el-button @click="handleCancel">取 消</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, getCurrentInstance, provide, watch } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectGlobalData } from '@/views/provide';
import { updateModes } from '../../../../apis/modes';

export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const commandFiles = require.context('../../plugins/', true, /Modes\.vue/);
        const commandModules = commandFiles.keys().map(c => commandFiles(c).default);
        const current = getCurrentInstance();
        const globalData = injectGlobalData();
        const modeState = ref({});
        provide('mode-state', modeState);

        const state = reactive({
            show: props.modelValue,
            loading: false,
            mode: '',
            mode1: '',
            items: computed(() => {
                const arr = globalData.value.usernames[globalData.value.username].Modes || [];
                if (arr.length > 0 && state.mode == '') {
                    state.mode = arr[0].Name;
                }
                const value = (globalData.value.usernames[globalData.value.username].Modes || []).filter(c => c.Name == state.mode)[0] || { Data: '{}' };
                modeState.value = value.Data;
                return arr;
            })
        });


        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleCancel = () => {
            state.show = false;
        }
        const handleSubmit = () => {
            state.show = false;
        }

        const edit = () => {
            const isEdit = state.mode1 != '';
            const title = isEdit ? `编辑【${state.mode1}】` : `增加新项`;
            ElMessageBox.prompt('输入名字', title, {
                confirmButtonText: '确认',
                cancelButtonText: '取消',
                inputValue: state.mode1
            }).then(({ value }) => {
                value = value.replace(/^\s|\s$/g, '');
                if (value) {

                    const modes = globalData.value.usernames[globalData.value.username].Modes;
                    if (isEdit) {
                        const mode = modes.filter(c => c.Name == state.mode1)[0];
                        mode.Name = value;
                    } else {
                        modes.push({ Name: value, Data: '{}' });
                    }
                    state.mode = value;
                    saveModes();
                }
            }).catch(() => { })
        }
        const handleAdd = () => {
            state.mode1 = '';
            edit();
        }
        const handleEdit = () => {
            state.mode1 = state.mode;
            edit();
        }
        const handleDelete = () => {
            const modes = globalData.value.usernames[globalData.value.username].Modes;
            const names = modes.map(c => c.Name);
            modes.splice(names.indexOf(state.mode), 1);
            state.mode = modes[0] || '';
            saveModes();
        }

        const saveModes = () => {
            updateModes({
                username: globalData.value.username,
                Data: globalData.value.usernames[globalData.value.username].Modes
            }).then(() => {
                state.loading = false;
                globalData.value.updateRuleFlag = Date.now();
                ElMessage.success('已操作')
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败')
            })
        }
        const handleSave = () => {
            if (!state.mode) {
                ElMessage.error('请选择一个模式');
                return;
            }
            const mode = globalData.value.usernames[globalData.value.username].Modes.filter(c => c.Name == state.mode)[0];
            if (!mode) {
                ElMessage.error('请选择一个模式');
                return;
            }

            const json = {};
            for (let i = 0; i < commandModules.length; i++) {
                json[commandModules[i].label] = current.refs[`mode-${commandModules[i].label}`][0].getData();
            }
            mode.Data = JSON.stringify(json);

            saveModes();
        }

        return {
            state, globalData, commandModules, handleCancel, handleSubmit, handleAdd, handleEdit, handleDelete, handleSave
        }
    }
}
</script>
<style lang="stylus">
.modes-setting-wrap {
    .el-tabs__content {
        flex: 1;
        overflow: auto;
    }
}
</style>
<style lang="stylus" scoped>
.modes-wrap {
    height: 70vh;
    position: relative;

    .head {
        padding-bottom: 1rem;
    }

    .plugins {
        // border: 1px solid #ddd;
        overflow: hidden;
        position: relative;

        .el-tab-pane {
            top: 10px;
            right: 10px;
            bottom: 10px;
            left: 10px;
        }
    }
}
</style>