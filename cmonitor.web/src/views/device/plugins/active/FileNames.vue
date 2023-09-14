<template>
    <el-dialog class="options" title="配置窗口列表" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="filenames-items-wrap flex flex-nowrap flex-column">
            <div class="head t-c flex">
                <el-button @click="handleAdd()">添加项</el-button>
                <span class="flex-1"></span>
                <span style="line-height:3.2rem">不允许打开哪些窗口</span>
            </div>
            <div class="flex-1">
                <div class="prevs-wrap">
                    <el-table :data="state.list" size="small" border stripe style="width: 100%" height="60vh">
                        <el-table-column prop="Desc" label="名称" />
                        <el-table-column prop="FileName" label="文件" />
                        <el-table-column label="操作" width="110">
                            <template #default="scope">
                                <el-button size="small" @click="handleAdd(scope.row)">
                                    <el-icon>
                                        <EditPen />
                                    </el-icon>
                                </el-button>
                                <el-popconfirm title="删除不可逆，是否确定?" @confirm="handleDel(scope.row)">
                                    <template #reference>
                                        <el-button size="small" type="danger">
                                            <el-icon>
                                                <Delete />
                                            </el-icon>
                                        </el-button>
                                    </template>
                                </el-popconfirm>
                            </template>
                        </el-table-column>
                    </el-table>
                </div>
            </div>
            <el-dialog :title="`${state.currentItem.ID==0?'添加项':'修改项'}`" destroy-on-close v-model="state.showEdit" center :close-on-click-modal="false" align-center width="80%">
                <div>
                    <p><el-input v-model="state.currentItem.Desc" size="large" placeholder="名称" /></p>
                    <p style="padding-top:1rem"><el-input v-model="state.currentItem.FileName" size="large" placeholder="文件,多个逗号间隔，无后缀则按标题处理" /></p>
                </div>
                <template #footer>
                    <el-button @click="handleEditCancel">取 消</el-button>
                    <el-button type="primary" :loading="state.loading" @click="handleEditSubmit">确 定</el-button>
                </template>
            </el-dialog>
        </div>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
import { injectGlobalData } from '@/views/provide';
import { activeAddExe, activeDelExe } from '@/apis/active';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    setup(props, { emit }) {

        const globalData = injectGlobalData();;
        const state = reactive({
            show: props.modelValue,
            loading: false,
            currentItem: { ID: 0, FileName: '', Desc: '' },
            showEdit: false,
            list: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user) {
                    return user.FileNames;
                }
                return [];
            })
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const handleAdd = (item) => {
            item = item || { ID: 0, FileName: '', Desc: '' };
            state.currentItem.FileName = item.FileName;
            state.currentItem.Desc = item.Desc;
            state.currentItem.ID = item.ID;
            state.showEdit = true;
        }
        const handleDel = (item) => {
            state.loading = true;
            activeDelExe(globalData.value.username, item.ID).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功!');
                    globalData.value.updateFlag = Date.now();
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error('操作失败!');
            })
        }
        const handleEditCancel = () => {
            state.showEdit = false;
        }
        const handleEditSubmit = () => {
            state.currentItem.Desc = state.currentItem.Desc.replace(/^\s|\s$/g, '');
            state.currentItem.FileName = state.currentItem.FileName.replace(/^\s|\s$/g, '');
            if (!state.currentItem.FileName || !state.currentItem.Desc) {
                return;
            }
            state.loading = true;
            activeAddExe({
                UserName: globalData.value.username,
                FileName: state.currentItem
            }).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功!');
                    state.showEdit = false;
                    globalData.value.updateFlag = Date.now();
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error('操作失败!');
            })
        }
        return { state, handleAdd, handleDel, handleEditCancel, handleEditSubmit }
    }
}
</script>

<style lang="stylus" scoped>
.filenames-items-wrap {
    .head {
        width: 100%;
        padding-bottom: 1rem;
    }

    .prevs-wrap {
        height: 100%;
        position: relative;
    }
}
</style>