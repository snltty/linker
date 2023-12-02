<template>
    <el-dialog class="options" title="使用互动答题" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
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
import { computed, watch } from '@vue/runtime-core';
import Item from './Item.vue'
import Info from './Info.vue'
import { injectPluginState } from '@/views/device/provide';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Item, Info },
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        console.log(pluginState.value);
        const running = computed(() => pluginState.value.ShareSnatch.Question.End == false);
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


        return {
            running, state, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>