<template>
    <div class="checkbox-wrap absolute flex flex-column">
        <div class="head flex">
            <span v-if="!state.single">
                <el-checkbox :indeterminate="state.isIndeterminate" v-model="state.checkAll" @change="handleCheckAllChange" :label="state.title" />
            </span>
            <span class="flex-1"></span>
            <slot name="title"></slot>
        </div>
        <div class="body flex-1 scrollbar">
            <el-checkbox-group :max="state.single?1:65535" v-model="state.checkList" @change="handleChange">
                <ul>
                    <template v-for="(item,index) in state.data" :key="index">
                        <li class="flex">
                            <div class="flex-1">
                                <el-checkbox :label="item[state.label]">
                                    <slot name="name" :item="item">
                                        {{item[state.text]}}
                                    </slot>
                                </el-checkbox>
                            </div>
                            <slot name="oper" :item="item"></slot>
                        </li>
                    </template>
                </ul>
            </el-checkbox-group>
        </div>
    </div>
</template>

<script>
import { computed, onMounted, reactive, watch } from 'vue'
export default {
    props: ['title', 'items', 'data', 'label', 'text', 'single'],
    emits: ['change'],
    setup(props, { emit }) {

        const state = reactive({
            title: props.title,
            label: props.label,
            text: props.text || props.label,
            single: props.single,
            data: computed(() => props.data),
            checkList: props.items.map(c => c[props.label]),
            checkAll: false,
            isIndeterminate: false
        });

        watch(() => props.items, () => {
            state.checkList = props.items.map(c => c[props.label]);
            updateCheckAll(state.checkList);
        })

        const handleCheckAllChange = (value) => {
            if (value) {
                state.checkList = state.data.map(c => c[state.label]);
            } else {
                state.checkList = [];
            }
            updateCheckAll(state.checkList);
        }
        const handleChange = (values) => {
            updateCheckAll(values);
        }
        const updateCheckAll = (values) => {
            const checkedCount = values.length;
            state.isIndeterminate = checkedCount > 0 && checkedCount < state.data.length;
            state.checkAll = checkedCount > 0 && checkedCount == state.data.length;
            emit('change', state.data.filter(c => state.checkList.indexOf(c[state.label]) >= 0));
        }
        onMounted(() => {
            updateCheckAll(state.checkList);
        });

        const getData = () => {
            return state.data.filter(c => state.checkList.indexOf(c[state.label]) >= 0);
        }


        return { state, handleCheckAllChange, handleChange, getData }
    }
}
</script>

<style lang="stylus" scoped>
.checkbox-wrap {
    border: 1px solid #ddd;
}

.head {
    border-bottom: 1px solid #ddd;
    line-height: 4rem;
    padding: 0 1rem;
    background-color: #fafafa;
}

.body {
    ul {
        padding: 1rem 0;
    }

    li {
        padding: 0 1rem;

        .el-checkbox {
            width: 100%;
            white-space: pre-wrap;
            word-break: break-all;
        }
    }
}
</style>