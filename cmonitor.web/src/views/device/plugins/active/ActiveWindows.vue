<template>
    <el-dialog class="options" title="当前打开窗口" destroy-on-close v-model="state.show" center align-center width="94%">
        <div class="wrap flex flex-column">
            <div class="inner flex-1 scrollbar">
                <ul>
                    <template v-for="(item,index) in state.list" :key="index">
                        <li class="flex">
                            <div class="flex-1">{{item.title}}</div>
                            <div class="btns">
                                <el-button type="danger" plain @click="handleCloseActive(item)">
                                    <el-icon>
                                        <Delete />
                                    </el-icon>
                                </el-button>
                            </div>
                        </li>
                    </template>
                </ul>
            </div>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="success" @click="handleCancel" plain>确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity';
import { onMounted, watch } from '@vue/runtime-core';
import { activeKill, getActiveWindows } from '../../../../apis/active'
import { injectPluginState } from '../../provide'
import { exec } from '@/apis/command';
import { ElMessage, ElMessageBox } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue'],
    components: {},
    setup(props, { emit }) {

        const pluginState = injectPluginState();
        const state = reactive({
            show: props.modelValue,
            list: [],
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const excludeArr = ['Microsoft Text Input Application', 'DragFileWindowTitle', 'TrafficMonitor', '安装', 'Program Manager'];
        const getData = () => {
            getActiveWindows(pluginState.value.activeWindow.devices[0].MachineName).then((res) => {
                const arr = Object.keys(res).map(c => {
                    return {
                        pid: c,
                        title: res[c]
                    }
                }).filter(c => excludeArr.indexOf(c.title) == -1);
                state.list = arr;
            }).catch((e) => {
            })
        }
        onMounted(() => {
            getData();
        });
        const handleCancel = () => {
            state.show = false;
        }
        const handleCloseActive = (item) => {
            const title = item.title;
            const pid = item.pid;
            ElMessageBox.confirm(`是否确定关闭焦点窗口？【${title}】`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                activeKill(pluginState.value.activeWindow.devices[0].MachineName, pid).then((res) => {
                    if (res) {
                        ElMessage.success('操作成功');
                        setTimeout(() => {
                            getData();
                        }, 1000);
                    } else {
                        ElMessage.error('操作失败');
                    }
                }).catch(() => {
                    ElMessage.error('操作失败');
                });
            }).catch(() => { });
        }

        return {
            state, handleCancel, handleCloseActive
        }
    }
}
</script>
<style lang="stylus" scoped>
.wrap {
    height: 70vh;

    .inner {
        border: 1px solid #ddd;
        border-radius: 4px;
        padding: 1rem 0;

        li {
            padding: 0.6rem;
            margin-bottom: 0.6rem;
            word-break: break-all;
            border-bottom: 1px solid #ddd;

            .btns {
                padding-left: 0.6rem;
            }
        }
    }
}
</style>