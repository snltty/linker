<template>
    <div class="rule-items-wrap flex flex-nowrap flex-column">
        <div class="head t-c flex">
            <el-button @click="handleAdd()">添加项</el-button>
        </div>
        <div class="flex-1">
            <div class="prevs-wrap">
                <el-table :data="state.list" size="small" border stripe style="width: 100%" height="50vh">
                    <el-table-column prop="Name" label="名称" />
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
import { updateRule } from '../../../../../apis/hijack'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {

        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            currentItem: { Name: '', Name1: '' },
            showEdit: false,
            list: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Rules) {
                    if (state.group == 0 && user.Rules.length > 0) {
                        state.group = user.Rules[0].Name;
                    }
                    return user.Rules;
                }
                return [];
            })
        });

        const handleAdd = (item) => {
            item = item || { Name: '', Name1: '' };
            state.currentItem.Name = item.Name;
            state.currentItem.Name1 = item.Name;
            state.showEdit = true;
        }

        const handleEditCancel = () => {
            state.showEdit = false;
        }

        const _updateRule = () => {
            const rules = globalData.value.usernames[globalData.value.username].Rules || [];
            state.loading = true;
            updateRule({
                username: globalData.value.username,
                Data: rules
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
            const rules = globalData.value.usernames[globalData.value.username].Rules || [];
            const names = rules.map(c => c.Name);
            rules.splice(names.indexOf(item.Name), 1);
            globalData.value.usernames[globalData.value.username].Rules = rules;
            _updateRule();
        }
        const handleEditSubmit = () => {
            state.currentItem.Name = state.currentItem.Name.replace(/^\s|\s$/g, '');
            if (!state.currentItem.Name) {
                return;
            }

            const rules = globalData.value.usernames[globalData.value.username].Rules || [];
            const names = rules.map(c => c.Name);
            let index = names.indexOf(state.currentItem.Name1);
            if (index == -1) {
                if (names.indexOf(state.currentItem.Name) >= 0) {
                    ElMessage.error('已存在同名');
                    return;
                }
                rules.push({ Name: state.currentItem.Name })
            } else {
                rules[index].Name = state.currentItem.Name;
            }
            globalData.value.usernames[globalData.value.username].Rules = rules;
            _updateRule();
        }

        return { state, handleAdd, handleDel, handleEditCancel, handleEditSubmit }
    }
}
</script>

<style lang="stylus" scoped>
.rule-items-wrap {
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