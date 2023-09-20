<template>
    <div>
        <a href="javascript:;" @click="handleRebotSystem">
            <el-icon>
                <Refresh />
            </el-icon>
        </a>
        <a href="javascript:;" @click="handleCloseSystem">
            <el-icon>
                <SwitchButton />
            </el-icon>
        </a>
        <a href="javascript:;" @click="handleCommand">
            <el-icon>
                <Position />
            </el-icon>
        </a>
    </div>
</template>

<script>
import { injectPluginState } from '../../provide'
import { ElMessage, ElMessageBox } from 'element-plus';
import { exec } from '@/apis/command';
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleCommand = () => {
            pluginState.value.command.items = [props.data];
            pluginState.value.command.showCommand = true;
        }

        const handleRebotSystem = () => {
            handleSendCommand('确定重启系统吗？', `shutdown -r -f -t 00`);
        }
        const handleCloseSystem = () => {
            handleSendCommand('确定关闭系统吗？', `shutdown -s -f -t 00`);
        }

        const handleSendCommand = (desc, command, value) => {
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                exec([props.data.MachineName], [command]).then((res) => {
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

        return {
            handleCommand, handleRebotSystem, handleCloseSystem
        }
    }
}
</script>

<style lang="stylus" scoped>
a {
    width: 2.4rem;
    height: 2.4rem;
    text-align: center;
    line-height: 2.8rem;
    margin-bottom: 0.6rem;
    display: block;
    font-size: 2rem;
    border-radius: 50%;
    border: 1px solid #3e5a6e;
    box-shadow: 0 0 4px rgba(255, 255, 255, 0.1);
    background-color: rgba(255, 255, 255, 0.5);
    color: #3e5a6e;
    transition: 0.3s;

    &:hover {
        box-shadow: 0 0 4px 2px rgba(255, 255, 255, 0.5);
    }
}
</style>