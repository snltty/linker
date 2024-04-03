<template>
    <div class="process-items-wrap flex flex-nowrap flex-column">
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
                    <el-table-column prop="Name" label="名称">
                        <template #default="scope">
                            <strong :class="`allow-type-${scope.row.AllowType}`">{{scope.row.Name}}</strong>
                        </template>
                    </el-table-column>
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
        <el-dialog :title="`${state.currentItem.Name1?'修改项':'添加项'}`" destroy-on-close v-model="state.showEdit" center :close-on-click-modal="false" align-center width="80%">
            <div>
                <div class="alert">
                    <p>1、黑名单优先</p>
                    <p>2、支持进程名，域名，ip(支持掩码)</p>
                    <p>3、进程，域名，后序截取判断</p>
                </div>
                <div style="padding-bottom:1rem">
                    <el-input v-model="state.currentItem.Name" size="large" placeholder="进程 | 域名 | ip(支持掩码/32)" />
                </div>
                <div class="t-c" style="padding-bottom:1rem">
                    <el-radio-group v-model="state.currentItem.DataType">
                        <el-radio :label="0">进程</el-radio>
                        <el-radio :label="1">域名</el-radio>
                        <el-radio :label="2">IP</el-radio>
                    </el-radio-group>
                </div>
                <div class="t-c">
                    <el-switch v-model="state.currentItem.AllowType" size="large" active-text="允许" inactive-text="阻止" :active-value="0" :inactive-value="1" />
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
import { computed, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
import { updateProcess } from '../../../../../apis/hijack'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {
        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            group: '',
            currentItem: { Name: '', Name1: '', AllowType: 1, DataType: 0 },
            showEdit: false,
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Processs) {
                    if (state.group == 0 && user.Processs.length > 0) {
                        state.group = user.Processs[0].Name;
                    }
                    return user.Processs;
                }
                return [];
            }),
            list: computed(() => {
                let group = state.groups.filter(c => c.Name == state.group)[0];
                if (group) return group.List;
                return [];
            })
        });
        watch(() => state.currentItem.Name, () => {
            handleNameChange(state.currentItem.Name);
        })

        const handleNameChange = (value) => {
            const isExe = /^.{0,}(\.exe)$/.test(value);
            const isip = /^((?:(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d?\d))(\/\d{1,})?$/.test(value);
            state.currentItem.DataType = isExe ? 0 : (isip ? 2 : 1);
            if (isip && value.indexOf('/') < 0) {
                state.currentItem.Name = state.currentItem.Name + '/32';
            }
        }
        const handleAdd = (item) => {
            item = item || { Name: '', Name1: '', AllowType: 1, DataType: 0 };
            state.currentItem.Name = item.Name;
            state.currentItem.Name1 = item.Name;
            state.currentItem.AllowType = item.AllowType;
            state.currentItem.DataType = item.DataType;
            state.showEdit = true;
        }

        const handleEditCancel = () => {
            state.showEdit = false;
        }
        const updateProcessGroup = () => {
            const processs = globalData.value.usernames[globalData.value.username].Processs || [];
            state.loading = true;
            updateProcess({
                username: globalData.value.username,
                Data: processs
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
            const processs = globalData.value.usernames[globalData.value.username].Processs || [];
            const group = processs.filter(c => c.Name == state.group)[0];
            const items = group.List;

            const names = items.map(c => c.Name);
            items.splice(names.indexOf(item.Name), 1);

            globalData.value.usernames[globalData.value.username].Processs = processs;

            updateProcessGroup();
        }

        const handleEditSubmit = () => {
            state.currentItem.Name = state.currentItem.Name.replace(/^\s|\s$/g, '');
            if (!state.currentItem.Name) {
                return;
            }

            const processs = globalData.value.usernames[globalData.value.username].Processs || [];
            const group = processs.filter(c => c.Name == state.group)[0];
            const items = group.List;
            const names = items.map(c => c.Name);

            let index = names.indexOf(state.currentItem.Name1);
            if (index == -1) {
                if (names.indexOf(state.currentItem.Name) >= 0) {
                    ElMessage.error('已存在同名');
                    return;
                }
                items.push({
                    Name: state.currentItem.Name,
                    Desc: state.currentItem.Desc,
                    AllowType: state.currentItem.AllowType,
                    DataType: state.currentItem.DataType,
                })
            } else {
                items[index].Name = state.currentItem.Name;
                items[index].Desc = state.currentItem.Desc;
                items[index].AllowType = state.currentItem.AllowType;
                items[index].DataType = state.currentItem.DataType;
            }
            globalData.value.usernames[globalData.value.username].Processs = processs;
            updateProcessGroup();
        }
        return { state, handleNameChange, handleAdd, handleDel, handleEditCancel, handleEditSubmit }
    }
}
</script>

<style lang="stylus" scoped>
.process-items-wrap {
    .head {
        width: 100%;
        padding-bottom: 1rem;
    }

    .prevs-wrap {
        height: 100%;
        position: relative;
    }

    .allow-type-0 {
        color: green;
    }

    .allow-type-1 {
        color: red;
    }

    .alert {
        background-color: rgba(255, 136, 0, 0.2);
        border: 1px solid #ddd;
        margin-bottom: 1rem;
        padding: 0.6rem;
        border-radius: 0.4rem;
    }
}
</style>