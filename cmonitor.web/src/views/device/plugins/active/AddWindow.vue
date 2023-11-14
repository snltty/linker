<template>
    <el-dialog title="添加项" append-to-body destroy-on-close v-model="state.show" center align-center width="80%">
        <div>
            <p>标题:【{{state.title}}】</p>
            <p>描述:【{{state.desc}}】</p>
            <p>文件:【{{state.filename}}】</p>
            <p class="t-c">
                <el-select v-model="state.group" placeholder="选择一个分组" style="width:13rem">
                    <el-option v-for="item in state.groups" :key="item.ID" :label="item.Name" :value="item.ID" />
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
import { activeAdd } from '@/apis/active';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const globalData = injectGlobalData();
        const state = reactive({
            show: props.modelValue,
            group: 0,
            title: '',
            desc: '',
            filename: '',
            groups: computed(() => {
                let user = globalData.value.usernames[globalData.value.username];
                if (user) {
                    if (user.Windows.length > 0 && state.group == 0) {
                        state.group = user.Windows[0].ID;
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
            activeAdd({
                username: globalData.value.username,
                GroupID: state.group,
                Item: {
                    ID: 0,
                    Name: state.filename,
                    Desc: state.desc
                }
            }).then((error) => {
                if (!error) {
                    ElMessage.success('操作成功');
                    state.show = false;
                    globalData.value.updateRuleFlag = Date.now();
                } else {
                    ElMessage.error(`操作失败:${error}`);
                }
            }).catch(() => {
                ElMessage.error('操作失败');
            });
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