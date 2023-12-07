<template>
    <el-dialog class="options" :title="running?'正在互动':'使用互动'" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <div style="border:1px solid #ddd;padding:.6rem">
            <Info v-if="running"></Info>
            <Item v-else></Item>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed, onMounted, onUnmounted, watch } from '@vue/runtime-core';
import Item from './Item.vue'
import Info from './Info.vue'
import { injectPluginState } from '@/views/device/provide';
import { injectGlobalData } from '@/views/provide';
import { getQuestion } from '@/apis/snatch';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Item, Info },
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const running = computed(() => pluginState.value.shareSnatch.question.Question.End == false);
        const state = reactive({
            show: props.modelValue,
            loading: false,
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleCancel = () => {
            state.show = false;
        }

        let timer = 0;
        const loadQuestion = () => {
            getQuestion(globalData.value.username).then((res) => {
                const question = pluginState.value.shareSnatch.question;
                if (res) {
                    console.log(res);
                    question.Question.Name = res.Question.Name;
                    question.Question.Cate = res.Question.Cate;
                    question.Question.Type = res.Question.Type;
                    question.Question.Question = res.Question.Question;
                    question.Question.Correct = res.Question.Correct;
                    question.Question.Option = res.Question.Option;
                    question.Question.Chance = res.Question.Chance;
                    question.Question.End = res.Question.End;
                    question.Question.Join = res.Question.Join;
                    question.Question.Right = res.Question.Right;
                    question.Question.Wrong = res.Question.Wrong;
                    question.Name = res.Name;
                    question.Names = res.Names;
                    question.Answers = res.Answers
                        .filter(c => c.Time > 0).sort((a, b) => a.Time - b.Time)
                        .concat(res.Answers.filter(c => c.Time == 0));
                } else {
                    question.Question.End = true;
                }
                timer = setTimeout(loadQuestion, 1000);
            }).catch(() => {
                timer = setTimeout(loadQuestion, 1000);
            });
        }
        onMounted(() => {
            loadQuestion();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        })

        return {
            running, state, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>