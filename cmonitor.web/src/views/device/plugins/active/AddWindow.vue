<template>
    <el-dialog title="添加项" append-to-body destroy-on-close v-model="state.show" center align-center width="80%">
        <div>
            <p>标题:【{{state.title}}】</p>
            <p>描述:【{{state.desc}}】</p>
            <p>文件:【{{state.filename}}】</p>
            <p class="t-c">
                <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                    <el-option v-for="item in state.groups" :key="item.Name" :label="item.Name" :value="item.Name" />
                </el-select>
            </p>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" plain :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { computed, onMounted, watch } from '@vue/runtime-core';
import { injectPluginState } from '../../provide'
import { injectGlobalData } from '@/views/provide';
import { ElMessage } from 'element-plus';
import { activeUpdate } from '@/apis/active';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const state = reactive({
            show: props.modelValue,
            group: '',
            title: '',
            desc: '',
            filename: '',
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user) {
                    if (user.Windows.length > 0 && state.group == '') {
                        state.group = user.Windows[0].Name;
                    }
                    return user.Windows;
                }
                return [];
            }),
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        onMounted(() => {
            const data = pluginState.value.activeWindow.devices[0];
            if (!data) return;

            const arr = data.ActiveWindow.FileName.split('\\');
            let fileName = arr[arr.length - 1];
            let desc = data.ActiveWindow.Desc;
            const title = data.ActiveWindow.Title;

            if (desc == 'Application Frame Host') {
                fileName = title;
                desc = title;
            }
            state.title = title;
            state.desc = desc;
            state.filename = fileName;
            state.showAdd = true;
        })

        const handleCancel = () => {
            state.show = false;
        }
        const handleSubmit = () => {

            const windows = globalData.value.usernames[globalData.value.username].Windows || [];
            const group = windows.filter(c => c.Name == state.group)[0];
            const items = group.List;
            const names = items.map(c => c.Name);

            if (names.indexOf(state.filename) >= 0) {
                ElMessage.error('已存在同名');
                return;
            }
            items.push({ Name: state.filename, Desc: state.desc });
            globalData.value.usernames[globalData.value.username].Windows = windows;

            activeUpdate({
                username: globalData.value.username,
                Data: windows
            }).then((error) => {
                state.show = false;
                if (error) {
                    ElMessage.error(error);
                } else {
                    ElMessage.success('操作成功!');
                    globalData.value.updateRuleFlag = Date.now();
                }
            }).catch((e) => {
                ElMessage.error('操作失败!');
            })
        }

        return {
            state, handleCancel, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap {
    height: 7;
    0vh;
}
</style>