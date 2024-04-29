<template>
    <el-col :span="3">
        <el-switch class="wallpaper" size="small" @click="handleWallpaper" :model-value="data.Wallpaper.Value" inline-prompt active-text="壁纸" inactive-text="壁纸" />
    </el-col>
</template>

<script>
import { wallpaperUpdate } from '@/apis/wallpaper';
import { ElMessage, ElMessageBox } from 'element-plus';
import { injectPluginState } from '../../provide';
export default {
    pluginName:'cmonitor.plugin.wallpaper.',
    sort: 3,
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const handleWallpaper = () => {
            let desc = props.data.Wallpaper.Value ? '确定关闭壁纸吗？' : '确定开启壁纸吗？';
            ElMessageBox.confirm(desc, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                wallpaperUpdate([props.data.MachineName], !props.data.Wallpaper.Value, `${window.location.origin}/bg.jpg`).then((res) => {
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
        return { data: props.data, handleWallpaper }
    }
}
</script>

<style lang="stylus" scoped></style>