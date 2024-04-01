<template>
    <div class="snatchs-items-wrap flex flex-nowrap flex-column">
        <div class="flex-1">
            <div class="prevs-wrap">
                <el-form ref="formDom" :rules="state.rules" :model="state.currentItem" label-width="0">
                    <el-form-item>
                        <el-row class="w-100">
                            <el-col :span="12">
                                <el-form-item>
                                    <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                                        <el-option v-for="item in state.groups" :key="item.ID" :label="item.Name" :value="item.ID" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :span="12">
                                <el-form-item>
                                    <el-select @change="handleItemChange" v-model="state.item" placeholder="选择一个模板" style="width:13rem">
                                        <el-option v-for="item in state.list" :key="item.ID" :label="item.Title" :value="item.ID" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item prop="Question">
                        <el-input type="textarea" resize="none" rows="4" v-model="state.currentItem.Question" placeholder="题目内容" maxlength="250" show-word-limit />
                    </el-form-item>

                    <el-form-item>
                        <el-row class="w-100">
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
                        <ul class="w-100">
                            <template v-for="(item,index) in state.currentItem.Options" :key="index">
                                <li class="flex" style="margin-bottom:.6rem">
                                    <span>{{String.fromCharCode(index+65)}}、</span>
                                    <div class="flex-1">
                                        <el-input style="width:98%;" maxlength="20" show-word-limit v-model="item.Text" placeholder="" />
                                    </div>
                                    <div>
                                        <el-checkbox v-if="state.currentItem.Cate==1" size="small" v-model="item.Value" label="答案" style="margin-right:.6rem" />
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
                    <el-form-item>
                        <div class="t-c w-100">
                            <el-button type="success" :loading="state.loading" @click="handleEditSubmit">确定开始</el-button>
                            <el-button type="warning" :loading="state.loading" @click="handleRandomSubmit">随机开始</el-button>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, onMounted } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus';
import { addQuestion, randomQuestion, updateQuestion } from '@/apis/snatch'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '@/views/device/provide';
export default {
    setup() {
        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            loading: false,
            group: 0,
            item: 0,
            currentItem: { ID: 0, Cate: 1, Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Chance: 65535 },
            rules: {
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
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user && user.Snatchs) {
                    if (state.group == 0 && user.Snatchs.length > 0) {
                        state.group = user.Snatchs[0].ID;
                    }
                    return user.Snatchs;
                }
                return [];
            }),
            list: computed(() => {
                let group = state.groups.filter(c => c.ID == state.group)[0];
                if (group) {
                    if (state.item == 0 && group.List.length > 0) {
                        state.item = group.List[0].ID;
                    }
                    return group.List;
                }
                return [];
            })
        });

        const handleCateChange = () => {
            if (state.currentItem.Cate == 2) {
                state.currentItem.Type = 1
            }
        }
        const handleItemChange = () => {
            const item = state.list.filter(c => c.ID == state.item)[0] || { ID: 0, Cate: 1, Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Chance: 65535 };
            state.currentItem.Cate = item.Cate;
            state.currentItem.Type = item.Type;
            state.currentItem.ID = item.ID;
            state.currentItem.Question = item.Question;
            state.currentItem.Options = item.Options;
            state.currentItem.Correct = item.Correct;
            state.currentItem.Chance = item.Chance;
        }
        onMounted(() => {
            handleItemChange();
        });

        const formDom = ref(null);
        const parseCorrect = (json) => {
            return json.Type == 1 ? json.Options.reduce((arr, value, index) => {
                if (value.Value) arr.push(String.fromCharCode(index + 65));
                return arr;
            }, []).join('') : json.Correct.replace(/^\s|\s$/g, '');
        }

        const handleEditSubmit = () => {
            ElMessageBox.confirm('确定以当前编辑好的题目开始吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                handleEdit();
            }).catch(() => { });
        }
        const handleEdit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return;
                }

                const names = pluginState.value.command.devices.map(c => c.MachineName);
                if (names.length == 0) {
                    ElMessage.error('请至少选择一个设备');
                    return;
                }

                const json = JSON.parse(JSON.stringify(state.currentItem));
                const chance = +json.Chance;
                const type = +json.Type;
                const cate = +json.Cate;
                const corrects = parseCorrect(json);
                const options = json.Options.map((value, index) => `${String.fromCharCode(index + 65)}、${value.Text}`).join('\r\n');
                const optionLength = json.Options.length;
                const questionCont = type == 1 ? `${json.Question}\r\n${options}` : json.Question;
                const username = globalData.value.username;

                if (cate == 1 && !corrects) {
                    ElMessage.error('没有正确答案');
                    return;
                }

                const question = {
                    cache: {
                        UserName: username,
                        MachineNames: names,
                    },
                    question: {
                        Type: type,
                        Cate: cate,
                        Question: questionCont,
                        Correct: corrects,
                        Option: optionLength,
                        Chance: chance,
                        UserName: username,
                    }
                }
                state.loading = true;
                addQuestion(question).then((res) => {
                    state.loading = false;
                    if (res) {
                        ElMessage.success('操作成功!');
                    } else {
                        ElMessage.error('操作失败!');
                    }
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error('操作失败!');
                });

            });
        }

        const handleRandomSubmit = () => {
            ElMessageBox.confirm('随机开始，则每台设备都以随机题目方式启动，确定以随机方式开始吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                handleRandom();
            }).catch(() => { });
        }
        const handleRandom = () => {
            const names = pluginState.value.command.devices.map(c => c.MachineName);
            state.loading = true;
            randomQuestion(names.length).then((questions) => {
                state.loading = false;
                if (questions.length == 0) {
                    ElMessage.error('没有可用题目');
                    return;
                }

                const username = globalData.value.username;
                const arr = names.map(name => {
                    const question = questions[parseInt(Math.random() * questions.length)];

                    const corrects = parseCorrect(question);
                    const options = question.Options.map((value, index) => `${String.fromCharCode(index + 65)}、${value.Text}`).join('\r\n');
                    const questionCont = question.Type == 1 ? `${question.Question}\r\n${options}` : question.Question;
                    return {
                        MachineName: name,
                        Question: {
                            UserName: username,
                            Cate: question.Cate,
                            Type: question.Type,
                            Chance: question.Chance,
                            Option: question.Options.length,
                            Question: questionCont,
                            Correct: corrects,
                        }
                    }
                });

                state.loading = true;
                addQuestion({
                    cache: {
                        UserName: username,
                        MachineNames: names,
                    },
                }).then(() => {
                    state.loading = false;
                    updateQuestion(username, arr);
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error('操作失败!' + e);
                })
            }).catch((e) => {
                state.loading = false;
                ElMessage.error('操作失败!' + e);
            })
        }

        const handleAddOption = (index) => {
            if (state.currentItem.Options.length >= 6) return;
            state.currentItem.Options.splice(index + 1, 0, { Text: '', Value: false });
        }
        const handleDelOption = (index) => {
            if (state.currentItem.Options.length <= 1) return;
            state.currentItem.Options.splice(index, 1);
        }
        return { state, formDom, handleEditSubmit, handleRandomSubmit, handleItemChange, handleAddOption, handleDelOption, handleCateChange }
    }
}
</script>

<style lang="stylus" scoped>
.snatchs-items-wrap {
    border: 1px solid #ddd;
    padding: 0.6rem;
    border-radius: 0.4rem;

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