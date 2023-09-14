<template>
    <div class="checkbox-wrap absolute flex flex-column">
        <div class="head flex">
            <span>
                {{state.title}}
            </span>
            <span class="flex-1"></span>
        </div>
        <div class="body flex-1 scrollbar">
            <slot name="wrap">
                <ul>
                    <template v-for="(item,index) in state.data" :key="index">
                        <li class="flex">
                            <slot :item="item">
                                <div class="default" @click="handleClick(item)">
                                    {{item}}
                                </div>
                            </slot>
                        </li>
                    </template>
                </ul>
            </slot>
        </div>
    </div>
</template>

<script>
import { computed, reactive } from 'vue'
export default {
    props: ['title', 'data'],
    setup(props, { emit }) {
        const state = reactive({
            title: props.title,
            data: computed(() => props.data),
        });
        const handleClick = (item) => {
            emit('prev', item);
        }
        return { state, handleClick }
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
        .el-checkbox {
            width: 100%;
        }

        &>div.default {
            padding: 0.6rem 1rem;
            line-height: 2rem;
        }

        &:hover {
            background-color: rgba(0, 0, 0, 0.1);
        }
    }
}
</style>