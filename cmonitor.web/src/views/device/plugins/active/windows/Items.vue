<template>
    <div class="windows-items-wrap flex flex-nowrap flex-column">
        <div class="head t-c flex">
            <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                <el-option v-for="item in state.groups" :key="item.Name" :label="item.Name" :value="item.Name" />
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
        <el-dialog :title="`${state.currentItem.Name1==0?'修改项':'添加项'}`" destroy-on-close v-model="state.showEdit" center :close-on-click-modal="false" align-center width="80%">
            <div>
                <p><el-input v-model="state.currentItem.Desc" size="large" placeholder="名称" /></p>
                <p style="padding-top:1rem"><el-input v-model="state.currentItem.Name" size="large" placeholder="规则" /></p>
                <div style="padding-top:1rem">
                    <p>1、带后缀的文件名</p>
                    <p>2、无后缀的标题</p>
                    <p>3、/xxx/格式的正则</p>
                </div>
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
import { activeUpdate } from '@/apis/active'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {
        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            group: '',
            currentItem: { Name: '', Name1: '', Desc: '' },
            showEdit: false,
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Windows) {
                    if (state.group == '' && user.Windows.length > 0) {
                        state.group = user.Windows[0].Name;
                    }
                    return user.Windows;
                }
                return [];
            }),
            list: computed(() => {
                let group = state.groups.filter(c => c.Name == state.group)[0];
                if (group) return group.List;
                return [];
            })
        });

        const handleAdd = (item) => {
            item = item || { Name: '', Name1: '', Desc: '' };
            state.currentItem.Name = item.Name;
            state.currentItem.Name1 = item.Name;
            state.currentItem.Desc = item.Desc;
            state.showEdit = true;
        }
        const handleEditCancel = () => {
            state.showEdit = false;
        }

        const updateActiveGroup = () => {
            const windows = globalData.value.usernames[globalData.value.username].Windows || [];
            state.loading = true;
            activeUpdate({
                username: globalData.value.username,
                Data: windows
            }).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    state.showEdit = false;
                    ElMessage.success('操作成功!');
                    globalData.value.updateRuleFlag = Date.now();
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error('操作失败!');
            })
        }

        const handleDel = (item) => {
            const windows = globalData.value.usernames[globalData.value.username].Windows || [];
            const group = windows.filter(c => c.Name == state.group)[0];
            const items = group.List;

            const names = items.map(c => c.Name);
            items.splice(names.indexOf(item.Name), 1);

            globalData.value.usernames[globalData.value.username].Windows = windows;

            updateActiveGroup();
        }

        const handleEditSubmit = () => {
            state.currentItem.Name = state.currentItem.Name.replace(/^\s|\s$/g, '');
            if (!state.currentItem.Name) {
                return;
            }

            const windows = globalData.value.usernames[globalData.value.username].Windows || [];
            const group = windows.filter(c => c.Name == state.group)[0];
            if(!group) return;
            const items = group.List;
            const names = items.map(c => c.Name);

            let index = names.indexOf(state.currentItem.Name1);
            if (index == -1) {
                if (names.indexOf(state.currentItem.Name) >= 0) {
                    ElMessage.error('已存在同名');
                    return;
                }
                items.push({ Name: state.currentItem.Name, Desc: state.currentItem.Desc })
            } else {
                items[index].Name = state.currentItem.Name;
                items[index].Desc = state.currentItem.Desc;
            }
            globalData.value.usernames[globalData.value.username].Windows = windows;
            updateActiveGroup();
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