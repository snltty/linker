<template>
    <el-dialog class="options" title="限制组配置" destroy-on-close v-model="state.show" center :close-on-click-modal="false" align-center width="94%">
        <el-tabs type="border-card">
            <el-tab-pane label="限制组">
                <Groups></Groups>
            </el-tab-pane>
            <el-tab-pane label="限制内容">
                <Rule></Rule>
            </el-tab-pane>
        </el-tabs>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { watch } from '@vue/runtime-core';
import Groups from './Groups.vue'
import Rule from './Rule.vue'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: { Groups, Rule },
    setup(props, { emit }) {

        const state = reactive({
            show: props.modelValue
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
            state, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>