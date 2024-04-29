<template>
    <div class="absolute flex flex-column flex-nowrap">
        <div class="flex">
            <el-checkbox v-model="state.use">使用规则</el-checkbox>
        </div>
        <div class="flex flex-1">
            <div class="private">
                <CheckBoxWrap  ref="privateExes" :data="state.privateExes" :items="state.ids1" label="Name" text="Name" title="私有窗口">
                </CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="public">
                <CheckBoxWrap ref="publicExes" :data="state.publicExes" :items="state.ids2" label="Name" text="Name" title="公共窗口">
                </CheckBoxWrap>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { computed, getCurrentInstance, inject, onMounted, watch,nextTick } from '@vue/runtime-core';
import CheckBoxWrap from '../../boxs/CheckBoxWrap.vue'
import { injectGlobalData } from '@/views/provide';
export default {
    label: '窗口',
    pluginName:'cmonitor.plugin.active.',
    components: { CheckBoxWrap },
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const user = computed(() => globalData.value.usernames[globalData.value.username]);
        const publicUserName = globalData.value.publicUserName;
        const publicUser = computed(() => globalData.value.usernames[publicUserName]);
        const usePublic = publicUser.value && globalData.value.username != publicUserName;
        const current = getCurrentInstance();
        const modeState = inject('mode-state');
        const state = reactive({
            use: false,
            privateExes: computed(() => user.value ? user.value.Windows || [] : []),
            publicExes: computed(() => usePublic ? publicUser.value.Windows || [] : []),
            ids1: [],
            ids2: [],
        });
        watch(() => modeState.value, () => {
            parseMode();
        });
        onMounted(() => { parseMode(); });
        const parseMode = () => {
            const json = JSON.parse(modeState.value)[current.type.label] || {};
            state.use = json.use || false;
            state.ids1 = (json.ids1 || []).map(c => { return { Name: c }; });
            state.ids2 = (json.ids2 || []).map(c => { return { Name: c }; });
        }

        const privateExes = ref(null);
        const publicExes = ref(null);
        const parseRule = () => {
            const _privateIds = privateExes.value.getData().map(c => c.Name);
            const _privateExes = state.privateExes.filter(c => _privateIds.indexOf(c.Name) >= 0);
            const _publicIds = publicExes.value.getData().map(c => c.Name);
            const _publicExes = state.publicExes.filter(c => _publicIds.indexOf(c.Name) >= 0);
            const exes = _privateExes.concat(_publicExes).reduce((data, item, index) => {
                let arr = item.List.reduce((val, item, index) => {
                    val = val.concat(item.Name.split(','));
                    return val;
                }, []);
                for (let i = 0; i < arr.length; i++) {
                    if (arr[i] && data.indexOf(arr[i]) == -1) {
                        data.push(arr[i]);
                    }
                }
                return data;
            }, []);

            return {
                path: 'active/disallow',
                use: state.use,
                ids1: _privateIds,
                ids2: _publicIds,
                list: exes
            };
        }

        const getData = () => {
            return parseRule();
        }

        return {
            state, getData, privateExes, publicExes
        }
    }
}
</script>
<style lang="stylus" scoped>
.private, .public {
    width: 49%;
    position: relative;
}
</style>