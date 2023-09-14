<template>
    <a v-if="data.ActiveWindow.Pid>0" class="process flex" href="javascript:;" @click="handleCloseActive">
        <span class="title flex-1">{{data.ActiveWindow.Title}}</span>
        <p class="btn">
            <a href="javascript:;" @click.stop="handleChoose">
                <el-icon>
                    <Warning />
                </el-icon>
                <span class="num">{{data.ActiveWindow.Count}}</span>
            </a>
            <a href="javascript:;" @click.stop="handleAddExe">
                <el-icon>
                    <CirclePlus />
                </el-icon>
            </a>
        </p>
    </a>
</template>

<script>
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '@/apis/command';
import { activeAddExe } from '@/apis/active';
import { injectGlobalData } from '@/views/provide';
import { injectPluginState } from '../../provide';
export default {
    props: ['data'],
    setup(props, { emit }) {

        const globalData = injectGlobalData();
        const pluginState = injectPluginState();
        const handleCloseActive = () => {
            ElMessageBox.confirm('是否确定关闭焦点窗口？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                exec([props.data.MachineName], [`taskkill /f /pid ${props.data.ActiveWindow.Pid}`]).then((res) => {
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

        const handleChoose = () => {
            pluginState.value.activeWindow.devices = [props.data];
            pluginState.value.activeWindow.showChoose = true;
        }
        const handleAddExe = () => {
            const arr = props.data.ActiveWindow.FileName.split('\\');
            const fileName = arr[arr.length - 1];
            const desc = props.data.ActiveWindow.Desc;
            const title = props.data.ActiveWindow.Title;
            let html = `</p>是否确定添加到待选列表？</p>`;
            html += `<p>标题:【${title}】</p>`;
            html += `<p>描述:【${desc}】</p>`;
            html += `<p>文件:【${fileName}】</p>`;
            html += `<p>windows商店应用，可能无法阻止，需要手动添加例如【计算器】，以侦测程序关闭</p>`;
            ElMessageBox.confirm(html, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                dangerouslyUseHTMLString: true,
                type: 'warning',
            }).then(() => {
                activeAddExe({
                    username: globalData.value.username,
                    FileName: {
                        ID: 0,
                        FileName: fileName,
                        Desc: desc
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

            }).catch(() => { });
        }

        return {
            data: props.data,
            handleCloseActive, handleChoose, handleAddExe
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
        line-height: 1.4rem;
    }

    .btn {
        a {
            font-size: 1.6rem;
            color: #adbfcc;
            margin-left: 0.6rem;
            position: relative;

            span.num {
                font-size: 1.3rem;
                position: absolute;
                right: 110%;
                top: -20%;
                color: #7c95a7;
            }
        }
    }
}
</style>