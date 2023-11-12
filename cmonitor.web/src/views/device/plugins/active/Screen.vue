<template>
    <a class="process flex" href="javascript:;" @click="handleClick">
        <span class="title flex-1">{{data.ActiveWindow.Title}}</span>
        <p class="btn">
            <a href="javascript:;" @click.stop="handleAddExe">
                <el-icon>
                    <CirclePlus />
                </el-icon>
            </a>
            <a href="javascript:;" @click.stop="handleShowWindows">
                <el-icon>
                    <Tickets />
                </el-icon>
                <span class="num">{{data.ActiveWindow.WindowCount}}</span>
            </a>
        </p>
    </a>
</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '@/apis/command';
import { injectPluginState } from '../../provide';
export default {
    props: ['data'],
    setup(props, { emit }) {

        const pluginState = injectPluginState();

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
        const handleAddExe = () => {
            pluginState.value.activeWindow.devices = [props.data];
            pluginState.value.activeWindow.showAddWindow = true;
        }
        const handleShowWindows = () => {
            pluginState.value.activeWindow.devices = [props.data];
            pluginState.value.activeWindow.showActiveWindows = true;
        }
        const handleTimes = () => {
            pluginState.value.activeWindow.devices = [props.data];
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
            data: props.data, handleAddExe, handleShowWindows, handleClick
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
                right: 50%;
                top: -50%;
                color: #fff;
                text-shadow: 0 0 1px rgba(255, 0, 0, 1);
            }
        }
    }
}
</style>