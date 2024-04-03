<template>
    <div class="absolute flex flex-column flex-nowrap">
        <div class="head">
            <el-checkbox v-model="state.use">使用规则</el-checkbox>
        </div>
        <div class="body flex-1 scrollbar">
            <ul>
                <template v-for="(item,index) in state.options" :key="index">
                    <li class="flex">
                        <span class="label">{{item.label}}</span>
                        <span class="flex-1"></span>
                        <div class="options">
                            <el-switch v-model="item.value" inline-prompt active-text="禁用" inactive-text="启用" size="large" />
                        </div>
                    </li>
                </template>
            </ul>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { getCurrentInstance, inject, onMounted, watch } from '@vue/runtime-core';
import { injectGlobalData } from '@/views/provide';
export default {
    label: '系统选项',
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const current = getCurrentInstance();
        const modeState = inject('mode-state');
        const state = reactive({
            use: false,
            options: []
        });
        watch(() => modeState.value, () => {
            parseMode();
        });

        const parseMode = () => {
            const json = JSON.parse(modeState.value)[current.type.label] || {};
            state.use = json.use || false;
            const list = (json.list || []).reduce((json, current) => {
                json[current.key] = current.value;
                return json;
            }, {});
            parseOptions(list);
        }
        const parseOptions = (list) => {
            const optionJson = globalData.value.allDevices.reduce((json, value, index) => {
                json = Object.assign(json, value.System.OptionKeys);
                return json;
            }, {});
            const keys = Object.keys(optionJson);
            const arr = keys.map(c => {
                const item = optionJson[c];
                return { key: c, label: item.Desc, index: item.Index, value: list[c] || false }
            }).filter(c => c.label).sort((a, b) => a.index - b.index);
            state.options = arr
        }

        onMounted(() => { parseMode(); });

        const getData = () => {
            return {
                path: 'system/registryoptions',
                use: state.use,
                ids1: state.options.filter(c => c.value).map(c => c.key),
                ids2: [],
                list: state.options.map(c => {
                    return {
                        key: c.key,
                        value: c.value
                    }
                }),
            }
        }

        return {
            state, getData
        }
    }
}
</script>
<style lang="stylus" scoped>
.el-switch {
    --el-switch-on-color: rgba(255, 0, 0, 0.8) !important;
}

.body {
    border: 1px solid #ddd;
    padding: 1rem;
}
</style>