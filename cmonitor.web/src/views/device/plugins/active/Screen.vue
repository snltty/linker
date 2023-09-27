<template>
    <a class="process flex" href="javascript:;" @click="handleClick">
        <span class="title flex-1">{{data.ActiveWindow.Title}}</span>
        <p class="btn">
            <a href="javascript:;" @click.stop="handleAddExe">
                <el-icon>
                    <CirclePlus />
                </el-icon>
            </a>
        </p>
        <el-dialog title="添加项" append-to-body destroy-on-close v-model="state.showAdd" center :close-on-click-modal="false" align-center width="80%">
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
                <el-button @click="handleEditCancel">取 消</el-button>
                <el-button type="success" plain :loading="state.loading" @click="handleEditSubmit">确 定</el-button>
            </template>
        </el-dialog>
    </a>
</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '@/apis/command';
import { activeAdd } from '@/apis/active';
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
import { computed, reactive } from 'vue';
export default {
    props: ['data'],
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const state = reactive({
            showAdd: false,
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
        })
        const handleCloseActive = () => {
            const title = props.data.ActiveWindow.Title;
            const pid = props.data.ActiveWindow.Pid;
            ElMessageBox.confirm(`是否确定关闭焦点窗口？【${title}】`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                exec([props.data.MachineName], [`taskkill /f /pid ${pid}`]).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败');
                    }
                }).catch(() => {
                    ElMessage.error('操作失败');
                });

            }).catch(() => { });
        }
        const handleEditCancel = () => {
            state.showAdd = false;
        }
        const handleEditSubmit = () => {
            activeAdd({
                username: globalData.value.username,
                GroupID: state.group,
                Item: {
                    ID: 0,
                    Name: state.filename,
                    Desc: state.desc
                }
            }).then((error) => {
                globalData.value.updateFlag = Date.now();
                if (!error) {
                    ElMessage.success('操作成功');
                } else {
                    ElMessage.error(`操作失败:${error}`);
                }
            }).catch(() => {
                ElMessage.error('操作失败');
            });
        }
        const handleAddExe = () => {
            const arr = props.data.ActiveWindow.FileName.split('\\');
            let fileName = arr[arr.length - 1];
            let desc = props.data.ActiveWindow.Desc;
            const title = props.data.ActiveWindow.Title;

            if (desc == 'Application Frame Host') {
                fileName = title;
                desc = title;
            }
            state.title = title;
            state.desc = desc;
            state.filename = fileName;
            state.showAdd = true;
        }

        const handleTimes = () => {
            pluginState.value.activeWindow.items = [props.data];
            pluginState.value.activeWindow.showTimes = true;
        }
        let timer = 0;
        const handleClick = () => {
            if (timer) {
                clearTimeout(timer);
                timer = 0;
                handleTimes();
                return;
            }
            timer = setTimeout(() => {
                timer = 0;
                handleCloseActive();
            }, 300);
        }

        return {
            data: props.data, state,
            handleCloseActive, handleAddExe, handleEditCancel, handleEditSubmit, handleTimes, handleClick
        }
    }
}
</script>

<style lang="stylus" scoped>
.process {
    position: absolute;
    left: 0rem;
    right: 0rem;
    bottom: 0.2rem;
    background-color: rgba(0, 0, 0, 0.4);
    padding: 0.6rem;

    .title {
        position: relative;
        z-index: 2;
        font-size: 1.4rem;
        color: #fff;
        word-break: break-all;
        border-radius: 4px;
    }

    .btn {
        a {
            font-size: 1.6rem;
            color: #fff;
            margin-left: 0.6rem;
            position: relative;

            span.num {
                font-size: 1.3rem;
                position: absolute;
                right: 110%;
                top: -20%;
                color: #fff;
            }
        }
    }
}
</style>