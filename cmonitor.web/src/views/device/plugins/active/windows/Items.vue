<template>
    <div class="windows-items-wrap flex flex-nowrap flex-column">
        <div class="head t-c flex">
            <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                <el-option v-for="item in state.groups" :key="item.ID" :label="item.Name" :value="item.ID" />
            </el-select>
            <span class="flex-1"></span>
            <el-button @click="handleAdd()">添加项</el-button>
        </div>
        <div class="flex-1">
            <div class="prevs-wrap">
                <el-table :data="state.list" size="small" border stripe style="width: 100%" height="50vh">
                    <el-table-column prop="Name" label="名称"></el-table-column>
                    <el-table-column prop="Desc" label="描述"></el-table-column>
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
                <p style="padding-top:1rem"><el-input v-model="state.currentItem.Name" size="large" placeholder="文件,多个逗号间隔，无后缀则按标题处理" /></p>
            </div>
            <template #footer>
                <el-button @click="handleEditCancel">取 消</el-button>
                <el-button type="success" plain :loading="state.loading" @click="handleEditSubmit">确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
import { activeAdd, activeDel } from '@/apis/active'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {
        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            group: 0,
            currentItem: { ID: 0, Name: '', Desc: '' },
            showEdit: false,
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user) {
                    if (state.group == 0 && user.Windows.length > 0) {
                        state.group = user.Windows[0].ID;
                    }
                    return user.Windows;
                }
                return [];
            }),
            list: computed(() => {
                let group = state.groups.filter(c => c.ID == state.group)[0];
                if (group) return group.List;
                return [];
            })
        });

        const handleAdd = (item) => {
            item = item || { Name: '', ID: 0, Desc: '' };
            state.currentItem.Name = item.Name;
            state.currentItem.Desc = item.Desc;
            state.currentItem.ID = item.ID;
            state.showEdit = true;
        }
        const handleDel = (item) => {
            state.loading = true;
            activeDel(globalData.value.username, state.group, item.ID).then((error) => {
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
            state.currentItem.Name = state.currentItem.Name.replace(/^\s|\s$/g, '');
            if (!state.currentItem.Name) {
                return;
            }
            state.loading = true;
            activeAdd({
                UserName: globalData.value.username,
                GroupID: state.group,
                Item: state.currentItem
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
.windows-items-wrap {
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