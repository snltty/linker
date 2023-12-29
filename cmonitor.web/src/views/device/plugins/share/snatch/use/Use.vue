<template>
    <el-dialog class="options" :title="running?'正在互动':'使用互动'" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <div>
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
        const running = computed(() => pluginState.value.shareSnatch.answers.length > 0);
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
            getQuestion(globalData.value.username).then((answers) => {
                pluginState.value.shareSnatch.answers = answers;
                timer = setTimeout(loadQuestion, 1000);
            }).catch((e) => {
                console.log(e);
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