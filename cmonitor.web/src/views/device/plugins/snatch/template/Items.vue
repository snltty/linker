<template>
    <div class="snatchs-items-wrap flex flex-nowrap flex-column">
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
                    <el-table-column prop="Title" label="名称"></el-table-column>
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
        <el-dialog :title="`${state.currentItem.Title1?'修改项':'添加项'}`" destroy-on-close v-model="state.showEdit" center :close-on-click-modal="false" align-center width="94%">
            <div>
                <el-form ref="formDom" :rules="state.rules" :model="state.currentItem" label-width="0">
                    <el-form-item prop="Title">
                        <el-input v-model="state.currentItem.Title" placeholder="名称" maxlength="30" show-word-limit />
                    </el-form-item>
                    <el-form-item prop="Question">
                        <el-input type="textarea" resize="none" rows="6" v-model="state.currentItem.Question" placeholder="题目内容" maxlength="250" show-word-limit />
                    </el-form-item>

                    <el-form-item>
                        <el-row>
                            <el-col :span="6">
                                <el-form-item prop="Cate">
                                    <el-select style="width:90%;" @change="handleCateChange" v-model="state.currentItem.Cate" placeholder="类别">
                                        <el-option v-for="item in state.cates" :key="item.value" :label="item.label" :value="item.value" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :span="8">
                                <el-form-item prop="Type">
                                    <el-select style="width:90%;" v-model="state.currentItem.Type" placeholder="类别">
                                        <el-option v-for="item in state.types" :key="item.value" :label="item.label" :value="item.value" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :span="10">
                                <el-form-item label="机会" label-width="5rem" prop="Chance">
                                    <el-input v-model="state.currentItem.Chance" placeholder="最多答题多少次" />
                                </el-form-item>
                            </el-col>

                        </el-row>
                    </el-form-item>
                    <el-form-item v-if="state.currentItem.Type == 1">
                        <ul style="width:100%;">
                            <template v-for="(item,index) in state.currentItem.Options" :key="index">
                                <li class="flex" style="margin-bottom:.6rem">
                                    <span>{{String.fromCharCode(index+65)}}、</span>
                                    <div class="flex-1">
                                        <el-input style="width:98%;" maxlength="20" show-word-limit v-model="item.Text" placeholder="" />
                                    </div>
                                    <!-- <span class="flex-1"></span> -->
                                    <div>
                                        <el-checkbox size="small" v-model="item.Value" label="答案" style="margin-right:.6rem" />
                                        <el-button size="small" @click="handleAddOption(index)"><el-icon>
                                                <Plus />
                                            </el-icon></el-button>
                                        <el-button style="margin-left:.6rem" size="small" @click="handleDelOption(index)"><el-icon>
                                                <Minus />
                                            </el-icon></el-button>
                                    </div>
                                </li>
                            </template>
                        </ul>
                    </el-form-item>
                    <el-form-item v-if="state.currentItem.Type == 2">
                        <el-input v-model="state.currentItem.Correct" maxlength="120" show-word-limit placeholder="简答题答案" />
                    </el-form-item>
                </el-form>
            </div>
            <template #footer>
                <el-button @click="handleEditCancel">取 消</el-button>
                <el-button type="success" plain :loading="state.loading" @click="handleEditSubmit">确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
import { add, del } from '@/apis/snatch'
import { injectGlobalData } from '@/views/provide';
export default {
    setup() {
        const globalData = injectGlobalData();;
        const state = reactive({
            loading: false,
            group: '',
            currentItem: { Title: '', Title1: '', Cate: 1, Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Chance: 65535 },
            rules: {
                Title: [
                    { required: true, message: '名称必填', trigger: 'blur' }
                ],
                Question: [
                    { required: true, message: '内容必填', trigger: 'blur' }
                ],
                Chance: [
                    { required: true, message: '内容必填', trigger: 'blur' }
                ],
            },
            cates: [
                { label: '答题', value: 1 },
                { label: '投票', value: 2 },
            ],
            types: computed(() => {
                return state.currentItem.Cate == 1 ? [
                    { label: '选择题', value: 1 },
                    { label: '简答题', value: 2 },
                ] : [{ label: '选择题', value: 1 }];
            }),
            showEdit: false,
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Snatchs) {
                    if (state.group == '' && user.Snatchs.length > 0) {
                        state.group = user.Snatchs[0].Name;
                    }
                    return user.Snatchs;
                }
                return [];
            }),
            list: computed(() => {
                let group = state.groups.filter(c => c.Name == state.group)[0];
                if (group) return group.List;
                return [];
            })
        });

        const handleCateChange = () => {
            if (state.currentItem.Cate == 2) {
                state.currentItem.Type = 1
            }
        }
        const handleAdd = (item) => {
            item = item || { Title: '', Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Chance: 65535 };
            state.currentItem.Title = item.Title;
            state.currentItem.Title1 = item.Title;
            state.currentItem.Type = item.Type;
            state.currentItem.Question = item.Question;
            state.currentItem.Options = item.Options;
            state.currentItem.Correct = item.Correct;
            state.currentItem.Chance = item.Chance;
            state.showEdit = true;
        }

        const handleEditCancel = () => {
            state.showEdit = false;
        }

        const updateSnatchGroup = () => {
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
            const snatchs = globalData.value.usernames[globalData.value.username].Snatchs || [];
            const group = snatchs.filter(c => c.Name == state.group)[0];
            const items = group.List;

            const names = items.map(c => c.Title);
            items.splice(names.indexOf(item.Title), 1);

            globalData.value.usernames[globalData.value.username].Snatchs = snatchs;

            updateSnatchGroup();
        }
        const formDom = ref(null);
        const handleEditSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) return;

                const snatchs = globalData.value.usernames[globalData.value.username].Snatchs || [];
                const group = snatchs.filter(c => c.Name == state.group)[0];
                const items = group.List;
                const names = items.map(c => c.Title);

                let index = names.indexOf(state.currentItem.Title1);
                const json = JSON.parse(JSON.stringify(state.currentItem));

                if (index == -1) {
                    if (names.indexOf(state.currentItem.Title) >= 0) {
                        ElMessage.error('已存在同名');
                        return;
                    }
                    items.push({ Chance: +json.Chance, Type: +json.Type, Title: json.Title.replace(/^\s|\s$/g, ''), Correct: json.Correct.replace(/^\s|\s$/g, '') })
                } else {
                    items[index].Chance = +json.Chance;
                    items[index].Type = +json.Type;
                    items[index].Title = json.Title.replace(/^\s|\s$/g, '');
                    items[index].Correct = json.Correct.replace(/^\s|\s$/g, '');
                }
                globalData.value.usernames[globalData.value.username].Snatchs = snatchs;
                updateSnatchGroup();
            });
        }

        const handleAddOption = (index) => {
            if (state.currentItem.Options.length >= 6) return;
            state.currentItem.Options.splice(index + 1, 0, { Text: '', Value: false });
        }
        const handleDelOption = (index) => {
            if (state.currentItem.Options.length <= 1) return;
            state.currentItem.Options.splice(index, 1);
        }
        return { state, formDom, handleAdd, handleDel, handleEditCancel, handleEditSubmit, handleAddOption, handleDelOption, handleCateChange }
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