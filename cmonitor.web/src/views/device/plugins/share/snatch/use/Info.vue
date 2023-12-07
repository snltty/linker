<template>
    <div class="snatchs-items-wrap flex flex-nowrap flex-column">
        <div class="flex-1">
            <div class="prevs-wrap">
                <el-form ref="formDom" label-width="0">

                    <el-form-item prop="Question">
                        <el-input readonly type="textarea" resize="none" rows="8" :value="state.currentItem.Question.Question" placeholder="题目内容" maxlength="250" show-word-limit />
                    </el-form-item>

                    <el-form-item style="border:1px solid #ddd;padding:.6rem">
                        <el-row class="w-100">
                            <el-col :span="8">
                                <el-form-item label="类型" label-width="4rem">
                                    <span>{{state.cates[state.currentItem.Question.Cate]}}</span>
                                </el-form-item>
                            </el-col>
                            <el-col :span="8">
                                <el-form-item label="类别" label-width="4rem">
                                    <span>{{state.types[state.currentItem.Question.Type]}}</span>
                                </el-form-item>
                            </el-col>
                            <el-col :span="8">
                                <el-form-item label="机会" label-width="4rem">
                                    <span>{{state.currentItem.Question.Chance}}</span>
                                </el-form-item>
                            </el-col>
                        </el-row>
                        <el-row class="w-100">
                            <el-col :span="8">
                                <el-form-item class="t-c" label="参与" label-width="4rem">{{state.currentItem.Question.Join}}</el-form-item>
                            </el-col>
                            <template v-if="state.currentItem.Question.Cate ==1">
                                <el-col :span="8">
                                    <el-form-item class="t-c" label="正确" label-width="4rem">{{state.currentItem.Question.Right}}</el-form-item>
                                </el-col>
                                <el-col :span="8">
                                    <el-form-item class="t-c" label="错误" label-width="4rem">{{state.currentItem.Question.Wrong}}</el-form-item>
                                </el-col>
                            </template>
                            <template v-else>
                                <el-col :span="8">
                                    <el-form-item class="t-c" label="已选" label-width="4rem">{{state.currentItem.Question.Right}}</el-form-item>
                                </el-col>
                                <el-col :span="8">
                                    <el-form-item class="t-c" label="未选" label-width="4rem">{{state.currentItem.Question.Wrong}}</el-form-item>
                                </el-col>
                            </template>
                        </el-row>
                        <el-row class="w-100" v-if="state.currentItem.Question.Cate ==2">
                            <el-col :span="24">
                                <ul class="vote-statis">
                                    <template v-for="(item,index) in state.voteStatis" :key="index">
                                        <li>
                                            <el-progress :percentage="item.percent" striped striped-flow>
                                                <span style="width:130px;display:block;" class="t-r">{{item.text}}、{{item.len}}、{{item.percent.toFixed(2)}}%</span>
                                            </el-progress>
                                        </li>
                                    </template>
                                </ul>
                            </el-col>
                        </el-row>
                    </el-form-item>
                    <el-form-item label="" label-width="0">
                        <el-table :data="state.currentItem.Answers" max-height="30vh" border style="width: 100%">
                            <el-table-column prop="Name" label="设备" />
                            <el-table-column prop="ResultStr" label="答案">
                                <template #default="scope">
                                    <template v-if="state.currentItem.Question.Cate == 1 && state.currentItem.Question.Type == 1">
                                        <span :class="`result-${scope.row.Result?'green':'red'} answer-${scope.row.State==1?'ask':'confirm'}`">{{scope.row.ResultStr}}</span>
                                    </template>
                                    <template v-else>
                                        <span>{{scope.row.ResultStr}}</span>
                                    </template>
                                </template>
                            </el-table-column>
                        </el-table>
                    </el-form-item>
                    <el-form-item>
                        <div class="t-c w-100">
                            <el-button type="danger" :loading="state.loading" @click="handleRemove">结束互动</el-button>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
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
            currentItem: computed(() => pluginState.value.shareSnatch.question),
            voteStatis: computed(() => {
                const arr = [];
                const answers = pluginState.value.shareSnatch.question.Answers;
                const question = pluginState.value.shareSnatch.question.Question;
                for (let index = 0; index < question.Option; index++) {
                    const optionText = String.fromCharCode(65 + index);
                    const len = answers.filter(c => c.ResultStr == optionText).length;
                    arr.push({
                        text: optionText,
                        percent: len / question.Join * 100,
                        len: len
                    });
                }
                return arr.sort((a, b) => b.len - a.len);
            }),
            rules: {},
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

<style lang="stylus" scoped>
.snatchs-items-wrap {
    .result-green {
        color: green;
    }

    .result-red {
        color: red;
    }

    .answer-confirm {
        font-weight: bold;
    }
}
</style>