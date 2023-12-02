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
                                        <el-option v-for="item in state.list" :key="item.ID" :label="item.Name" :value="item.ID" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item prop="Name">
                        <el-input v-model="state.currentItem.Name" placeholder="名称" maxlength="30" show-word-limit />
                    </el-form-item>
                    <el-form-item prop="Question">
                        <el-input type="textarea" resize="none" rows="4" v-model="state.currentItem.Question" placeholder="题目内容" maxlength="250" show-word-limit />
                    </el-form-item>

                    <el-form-item>
                        <el-row class="w-100">
                            <el-col :span="8">
                                <el-form-item prop="Type">
                                    <el-select style="width:90%;" v-model="state.currentItem.Type" placeholder="类别">
                                        <el-option v-for="item in state.types" :key="item.value" :label="item.label" :value="item.value" />
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :span="8">
                                <el-form-item label-width="0" prop="Repeat">
                                    <el-checkbox v-model="state.currentItem.Repeat" label="重复答题" />
                                </el-form-item>
                            </el-col>
                            <el-col :span="8">
                                <el-form-item label="上限" label-width="4rem" prop="Max">
                                    <el-input v-model="state.currentItem.Max" placeholder="最多答题多少次" />
                                </el-form-item>
                            </el-col>

                        </el-row>
                    </el-form-item>
                    <el-form-item v-if="state.currentItem.Type == 1">
                        <ul class="w-100">
                            <template v-for="(item,index) in state.currentItem.Options" :key="index">
                                <li class="flex" style="margin-bottom:.6rem">
                                    <span>{{String.fromCharCode(index+65)}}、</span>
                                    <el-input style="width:11rem;" v-model="item.Text" placeholder="" />
                                    <span class="flex-1"></span>
                                    <div>
                                        <el-checkbox size="small" v-model="item.Value" label="答案" style="margin-right:.6rem" />
                                        <el-button size="small" @click="handleAddOption(index)">添加</el-button>
                                        <el-button size="small" @click="handleDelOption(index)">删除</el-button>
                                    </div>
                                </li>
                            </template>
                        </ul>
                    </el-form-item>
                    <el-form-item v-if="state.currentItem.Type == 2">
                        <el-input v-model="state.currentItem.Correct" placeholder="简答题答案" />
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
import { update } from '@/apis/snatch'
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
            currentItem: { ID: 0, Name: '', Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Max: 65535, Repeat: true },
            rules: {
                Name: [
                    { required: true, message: '名称必填', trigger: 'blur' }
                ],
                Question: [
                    { required: true, message: '内容必填', trigger: 'blur' }
                ],
            },
            types: [
                { label: '选择题', value: 1 },
                { label: '简答题', value: 2 },
            ],
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

        const handleItemChange = () => {
            const item = state.list.filter(c => c.ID == state.item)[0] || { ID: 0, Name: '', Type: 1, Question: '', Options: [{ Text: '', Value: false }], Correct: '', Max: 65535, Repeat: true };
            state.currentItem.Type = item.Type;
            state.currentItem.ID = item.ID;
            state.currentItem.Name = item.Name;
            state.currentItem.Question = item.Question;
            state.currentItem.Options = item.Options;
            state.currentItem.Correct = item.Correct;
            state.currentItem.Max = item.Max;
            state.currentItem.Repeat = item.Repeat;
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
                json.Max = +json.Max;
                json.Type = +json.Type;
                json.Name = json.Name.replace(/^\s|\s$/g, '');
                json.Correct = json.Correct.replace(/^\s|\s$/g, '');
                const corrects = json.Options.reduce((arr, value, index) => {
                    if (value.Value) arr.push(String.fromCharCode(index + 65));
                    return arr;
                }, []).join('');

                if (json.Type == 1 && corrects.length <= 0) {
                    ElMessage.error('至少有一个正确答案');
                    return;
                } else if (json.Type == 2 && !json.Correct) {
                    ElMessage.error('请输入正确答案');
                    return;
                }

                const question = {
                    Type: json.Type,
                    Question: json.Question,
                    Correct: json.Type == 1 ? corrects : json.Correct,
                    Option: json.Options.length,
                    Max: json.Max,
                    End: false,
                    Repeat: json.Repeat,
                    Join: names.length,
                    Right: 0,
                    Wrong: 0,
                }

                state.loading = true;
                update(names, question).then((res) => {
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
        return { state, formDom, handleEditSubmit, handleItemChange, handleAddOption, handleDelOption }
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