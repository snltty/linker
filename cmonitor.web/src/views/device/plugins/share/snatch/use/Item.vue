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
import { ElMessage } from 'element-plus';
import { addQuestion } from '@/apis/snatch'
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
        const handleEditSubmit = () => {
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
                json.Chance = +json.Chance;
                json.Type = +json.Type;
                json.Cate = +json.Cate;
                json.Correct = json.Correct.replace(/^\s|\s$/g, '');
                const corrects = json.Options.reduce((arr, value, index) => {
                    if (value.Value) arr.push(String.fromCharCode(index + 65));
                    return arr;
                }, []).join('');

                if (json.Cate == 1 && json.Type == 1 && corrects.length <= 0) {
                    ElMessage.error('至少有一个正确答案');
                    return;
                } else if (json.Type == 2 && !json.Correct) {
                    ElMessage.error('请输入正确答案');
                    return;
                }
                if (json.Cate == 2) {
                    json.Chance = 1;
                }
                const options = json.Options.map((value, index) => `${String.fromCharCode(index + 65)}、${value.Text}`).join('\r\n');

                const question = {
                    question: {
                        Type: json.Type,
                        Cate: json.Cate,
                        Question: `${json.Question}\r\n${options}`,
                        Correct: json.Type == 1 ? corrects : json.Correct,
                        Option: json.Options.length,
                        Chance: json.Chance,
                        End: false,
                        Join: names.length,
                        Right: 0,
                        Wrong: 0,
                    },
                    Name: globalData.value.username,
                    Names: names,
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

        const handleAddOption = (index) => {
            if (state.currentItem.Options.length >= 6) return;
            state.currentItem.Options.splice(index + 1, 0, { Text: '', Value: false });
        }
        const handleDelOption = (index) => {
            if (state.currentItem.Options.length <= 1) return;
            state.currentItem.Options.splice(index, 1);
        }
        return { state, formDom, handleEditSubmit, handleItemChange, handleAddOption, handleDelOption, handleCateChange }
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