<template>
    <div class="command-wrap flex flex-column">
        <div class="head t-c flex">
            <div>
                <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                    <el-option v-for="item in state.groups" :key="item.Name" :label="item.Name" :value="item.Name" />
                </el-select>
            </div>
            <div class="flex-1"></div>
            <div>
                <el-button @click="handleSave()" :loading="state.loading">保存选择</el-button>
            </div>
        </div>
        <div class="body flex flex-1">
            <div class="private">
                <CheckBoxWrap ref="privateProcess" :data="state.privateProcess" :items="state.privateProcessItems" label="Name" text="Name" title="私有程序组"></CheckBoxWrap>
            </div>
            <div class="flex-1"></div>
            <div class="public">
                <CheckBoxWrap ref="publicProcess" :data="state.publicProcess" :items="state.publicProcessItems" label="Name" text="Name" title="公共程序组"></CheckBoxWrap>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref, watch } from 'vue';
import CheckBoxWrap from '../../../boxs/CheckBoxWrap.vue'
import { ElMessage } from 'element-plus';
import { updateRule } from '../../../../../apis/hijack'
import { injectGlobalData } from '@/views/provide';
export default {
    components: { CheckBoxWrap },
    setup() {

        const globalData = injectGlobalData();;
        const user = computed(() => globalData.value.usernames[globalData.value.username]);
        const publicUserName = globalData.value.publicUserName;
        const publicUser = computed(() => globalData.value.usernames[publicUserName]);
        const usePublic = publicUser.value && globalData.value.username != publicUserName;

        const state = reactive({
            loading: false,
            group: '',
            groups: computed(() => {
                if (user.value && user.value.Rules) {
                    if (state.group == '' && user.value.Rules.length > 0) {
                        state.group = user.value.Rules[0].Name;
                    }
                    return user.value.Rules;
                }
                return [];
            }),
            rule: computed(() => {
                if (user && user.value.Rules) {
                    let rule = user.value.Rules.filter(c => c.Name == state.group)[0];
                    if (rule) {
                        console.log(rule);
                        console.log(user.value.Processs.filter(c => rule.PrivateProcesss.indexOf(c.Name) >= 0));
                        return rule;
                    }
                }
                return {
                    Name: '',
                    PrivateProcesss: [],
                    PublicProcesss: [],
                }
            }),
            privateProcess: computed(() => user.value ? user.value.Processs : []),
            privateProcessItems: computed(() => user.value ? user.value.Processs.filter(c => state.rule.PrivateProcesss.indexOf(c.Name) >= 0) : []),
            publicProcess: computed(() => usePublic ? publicUser.value.Processs : []),
            publicProcessItems: computed(() => usePublic ? publicUser.value.Processs.filter(c => state.rule.PublicProcesss.indexOf(c.Name) >= 0) : []),
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const privateProcess = ref(null);
        const publicProcess = ref(null);

        const _updateRule = () => {
            const rules = globalData.value.usernames[globalData.value.username].Rules || [];
            rules.forEach(element => {
                element.PrivateProcesss = element.PrivateProcesss.filter(c => typeof (c) != 'number');
                element.PublicProcesss = element.PublicProcesss.filter(c => typeof (c) != 'number');
            });
            state.loading = true;
            updateRule({
                username: globalData.value.username,
                Data: rules
            }).then((error) => {
                state.loading = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    state.showEdit = false;
                    ElMessage.success('操作成功!');
                    globalData.value.updateRuleFlag = Date.now();
                }
            }).catch((e) => {
                state.loading = false;
                ElMessage.error('操作失败!');
            })
        }
        const handleSave = () => {
            let rule = user.value.Rules.filter(c => c.Name == state.group)[0];
            if (!rule) {
                ElMessage.error('未选择任何限制分组');
                return;
            }
            rule.PrivateProcesss = privateProcess.value.getData().map(c => c.Name);
            rule.PublicProcesss = publicProcess.value.getData().map(c => c.Name);
            _updateRule();
        }

        return {
            state, handleSave, privateProcess, publicProcess
        }
    }
}
</script>

<style lang="stylus" scoped>
.command-wrap {
    height: 55vh;

    .head {
        width: 100%;
        padding-bottom: 1rem;
    }

    .private, .public {
        width: 49%;
        position: relative;
    }

    .process {
        height: 100%;
        width: 48%;
        position: relative;
    }
}
</style>