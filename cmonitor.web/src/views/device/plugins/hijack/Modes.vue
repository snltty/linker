<template>
    <div class="rule-wrap absolute flex flex-column flex-nowrap">
        <div class="flex">
            <el-checkbox v-model="state.use">使用规则</el-checkbox>
            <el-checkbox v-model="state.domainKill">暴力强杀(关闭应用程序)</el-checkbox>
        </div>
        <div class="rules flex-1 flex">
            <div class="private">
                <CheckBoxWrap  ref="privateRules" :data="state.privateRules" :items="state.ids1" label="Name" text="Name" title="私有限制"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="public">
                <CheckBoxWrap ref="publicRules" :data="state.publicRules" :items="state.ids2" label="Name" text="Name" title="公共限制"></CheckBoxWrap>
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
    pluginName:'cmonitor.plugin.hijack.',
    label: '网络',
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
            privateRules: computed(() => user.value ? user.value.Processs || [] : []),
            publicRules: computed(() => usePublic ? publicUser.value.Processs || [] : []),
            showids1:true,
            ids1: [],
            ids2: [],
        });
        watch(() => modeState.value, () => {
            parseMode();
        });
        onMounted(() => { parseMode(); });
        const parseMode = () => {
            const json = JSON.parse(modeState.value)[current.type.label] || { list: {} };
            state.use = json.use || false;
            state.ids1 = (json.ids1 || []).map(c => { return { Name: c }; });
            state.ids2 = (json.ids2 || []).map(c => { return { Name: c }; });
            state.domainKill = json.list.DomainKill || false;
        }


        const privateRules = ref(null);
        const publicRules = ref(null);
        const parseRule = () => {
            const _privateRules = privateRules.value.getData().map(c => c.Name);
            const _publicRules = publicRules.value.getData().map(c => c.Name);
            const _user = user.value;
            const _publicUser = publicUser.value;
            const publicList = (_user.Processs||[]).filter(c => _privateRules.indexOf(c.Name) >= 0);
            const privateList = [_publicUser.Processs || []].filter(c => _publicRules.indexOf(c.Name) >= 0);

            const origin = publicList.concat(privateList).reduce((arr, value, index) => {
                arr = arr.concat(value.List);
                return arr;
            }, []);
            const res = [];
            origin.forEach(element => {
                if (res.filter(c => c.Name == element.Name && c.DataType == element.DataType && c.AllowType == element.AllowType).length == 0) {
                    res.push(element);
                }
            });

            return {
                path: 'hijack/usehijackrules',
                use: state.use,
                ids1: _privateRules,
                ids2: _publicRules,
                list: {
                    AllowProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 0).map(c => c.Name),
                    DeniedProcesss: res.filter(c => c.DataType == 0 && c.AllowType == 1).map(c => c.Name),
                    AllowDomains: res.filter(c => c.DataType == 1 && c.AllowType == 0).map(c => c.Name),
                    DeniedDomains: res.filter(c => c.DataType == 1 && c.AllowType == 1).map(c => c.Name),
                    AllowIPs: res.filter(c => c.DataType == 2 && c.AllowType == 0).map(c => c.Name),
                    DeniedIPs: res.filter(c => c.DataType == 2 && c.AllowType == 1).map(c => c.Name),
                    DomainKill: state.domainKill
                }
            }
        }

        const getData = () => {
            return parseRule();
        }

        return {
            state, getData, globalData, privateRules, publicRules,
        }
    }
}
</script>
<style lang="stylus" scoped>
.rule-wrap {
    .rules {
        position: relative;

        .private, .public {
            width: 49%;
            position: relative;
        }
    }
}
</style>