<template>
    <div class="snatchs-items-wrap-info flex flex-nowrap flex-column">
        <div class="flex-1">
            <div class="prevs-wrap">
                <ul>
                    <template v-for="(group,index) in state.answers" :key="index">
                        <li>
                            <dl>
                                <dt>
                                    <el-input :value="group.Question.Question" :rows="4" type="textarea" resize="none" readonly />
                                </dt>
                                <dd class="right">
                                    <el-row class="w-100">
                                        <el-col :span="8">
                                            <el-form-item class="t-c" label="参与" label-width="4rem">{{group.Question.Join}}</el-form-item>
                                        </el-col>
                                        <template v-if="group.Question.Cate ==1">
                                            <el-col :span="8">
                                                <el-form-item class="t-c" label="正确" label-width="4rem">{{group.Question.Right}}</el-form-item>
                                            </el-col>
                                            <el-col :span="8">
                                                <el-form-item class="t-c" label="错误" label-width="4rem">{{group.Question.Wrong}}</el-form-item>
                                            </el-col>
                                        </template>
                                        <template v-else>
                                            <el-col :span="8">
                                                <el-form-item class="t-c" label="已选" label-width="4rem">{{group.Question.Right}}</el-form-item>
                                            </el-col>
                                            <el-col :span="8">
                                                <el-form-item class="t-c" label="未选" label-width="4rem">{{group.Question.Wrong}}</el-form-item>
                                            </el-col>
                                        </template>
                                    </el-row>
                                </dd>
                                <dd v-if="group.statis" class="padding statis">
                                    <ul>
                                        <template v-for="(item,index) in group.statis" :key="index">
                                            <li>
                                                <el-progress :percentage="item.percent" striped striped-flow>
                                                    <span style="width:130px;display:block;" class="t-r">{{item.text}}、{{item.len}}、{{item.percent.toFixed(2)}}%</span>
                                                </el-progress>
                                            </li>
                                        </template>
                                    </ul>
                                </dd>
                                <dd class="padding">
                                    <ul class="machine">
                                        <template v-for="(answer,aindex) in group.Answers" :key="aindex">
                                            <li :class="`flex answer-${group.Question.Cate} result-${answer.Result?'green':'red'} answer-${answer.State==1?'ask':'confirm'}`">
                                                <span class="name">{{answer.MachineName}}</span><span class="flex-1 t-r">{{answer.ResultStr}}</span>
                                            </li>
                                        </template>
                                    </ul>
                                </dd>
                            </dl>
                        </li>
                    </template>
                </ul>
            </div>
        </div>
        <div class="t-c w-100">
            <el-button type="danger" :loading="state.loading" @click="handleRemove">结束互动</el-button>
        </div>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed } from '@vue/runtime-core';
import { ElMessage, ElMessageBox } from 'element-plus';
import { removeQuestion } from '@/apis/snatch'
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '@/views/device/provide';
export default {
    setup() {
        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            loading: false,
            answers: computed(() => {
                const groups = pluginState.value.shareSnatch.answers;

                for (let i = 0; i < groups.length; i++) {
                    const answers = groups[i].Answers;
                    const question = groups[i].Question;

                    if (question.Cate == 2) {
                        const arr = new Array(question.Option);
                        arr.fill(0);
                        groups[i].statis = arr.map((value, index) => {
                            const optionText = String.fromCharCode(65 + index);
                            const len = answers.filter(c => c.ResultStr == optionText).length;
                            return { text: optionText, percent: len / answers.length * 100, len: len };
                        });
                    }
                    if (answers.length > 1) {
                        groups[i].Answers = answers.filter(c => c.Time > 0).sort((a, b) => a.Time - b.Time).concat(answers.filter(c => c.Time == 0));
                    }
                }
                return groups;
            }),
            types: [
                '',
                '选择题',
                '简答题',
            ],
            cates: [
                '',
                '答题',
                '投票',
            ],
        });
        const formDom = ref(null);
        const handleRemove = () => {
            ElMessageBox.confirm('确定结束互动吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                state.loading = true;
                removeQuestion(globalData.value.username).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        state.loading = false;
                        ElMessage.error('操作失败');
                    }
                }).catch(() => {
                    state.loading = false;
                    ElMessage.error('操作失败');
                });
            }).catch(() => { });
        }

        return { state, formDom, handleRemove }
    }
}
</script>
<style lang="stylus">
.snatchs-items-wrap-info textarea {
    box-shadow: none !important;
}
</style>

<style lang="stylus" scoped>
.snatchs-items-wrap-info {
    .prevs-wrap {
        height: 70vh;

        &>ul>li {
            border: 1px solid #ddd;
            border-radius: 0.4rem;

            dd {
                border-top: 1px solid #ddd;

                &.padding {
                    padding: 0.6rem;
                }

                &.right {
                    padding: 0 0.6rem;
                    background-color: #f5f5f5;
                }

                &.statis {
                    background-color: #fafafa;
                }
            }
        }

        ul.machine {
            li {
                border-top: 1px solid #ddd;
                padding: 0.4rem;

                &:first-child {
                    border-top: 0;
                }

                span.name {
                    padding-right: 0.6rem;
                }
            }
        }
    }

    .result-green.answer-confirm.answer-1 {
        color: green;
    }

    .result-red.answer-confirm.answer-1 {
        color: red;
    }

    .answer-confirm {
        font-weight: bold;
    }
}
</style>