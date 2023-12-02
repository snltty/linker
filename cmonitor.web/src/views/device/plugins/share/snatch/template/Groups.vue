<template>
    <div class="snatchs-items-wrap flex flex-nowrap flex-column">
        <div class="head t-c flex">
            <el-button @click="handleAdd()">添加项</el-button>
        </div>
        <div class="flex-1">
            <div class="prevs-wrap">
                <el-table :data="state.list" size="small" border stripe style="width: 100%" height="50vh">
                    <el-table-column prop="Name" label="名称" />
                    <el-table-column label="操作" width="110">
                        <template #default="scope">
                            <template v-if="scope.row.ID > 1">
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
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <el-dialog :title="`${state.currentItem.ID==0?'添加项':'修改项'}`" destroy-on-close v-model="state.showEdit" center :close-on-click-modal="false" align-center width="80%">
            <div>
                <el-input v-model="state.currentItem.Name" size="large" placeholder="分组名称" />
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
import { addGroup, delGroup } from '@/apis/snatch'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {

        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            currentItem: { ID: 0, Name: '' },
            showEdit: false,
            list: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Snatchs) {
                    if (state.group == 0 && user.Snatchs.length > 0) {
                        state.group = user.Snatchs[0].ID;
                    }
                    return user.Snatchs;
                }
                return [];
            })
        });

        const handleAdd = (item) => {
            item = item || { Name: '', ID: 0 };
            state.currentItem.Name = item.Name;
            state.currentItem.ID = item.ID;
            state.showEdit = true;
        }
        const handleDel = (item) => {
            state.loading = true;
            delGroup(globalData.value.username, item.ID).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功!');
                    globalData.value.updateRuleFlag = Date.now();
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
            addGroup({
                UserName: globalData.value.username,
                Group: state.currentItem
            }).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功!');
                    state.showEdit = false;
                    globalData.value.updateRuleFlag = Date.now();
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
.snatchs-items-wrap {
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