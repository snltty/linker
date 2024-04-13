<template>
    <div class="viewer-share-wrap" v-if="data.Viewer.share && data.Viewer.mode == 'server'">
        <div class="inner">
            <h5>
                <span>正在共享屏幕</span>
            </h5>
            <div>
                <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="确认结束共享吗?" @confirm="handleConfirm">
                    <template #reference>
                        <el-button :loading="state.loading" type="danger" plain round size="small">结束共享</el-button>
                    </template>
                </el-popconfirm>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive } from 'vue'
import { ElMessage } from 'element-plus';
import { injectPluginState } from '../../provide';
import { viewerUpdate } from '../../../../apis/viewer';
export default {
    props: ['data'],
    setup(props) {

        const pluginState = injectPluginState();
        const state = reactive({
            loading: false
        });
        const handleConfirm = () => {
            viewerUpdate({
                open: false,
                server: props.data.MachineName,
                shareid:props.data.Viewer.id
            }).then(() => {
                ElMessage.success('已操作！')
            }).catch(() => {
                ElMessage.error('操作失败！')
            });
        }

        return {
            data: props.data, state, handleConfirm
        }
    }
}
</script>

<style lang="stylus" scoped>
.viewer-share-wrap {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;

    .inner {
        position: absolute;
        left: 50%;
        top: 50%;
        transform: translateX(-50%) translateY(-70%);
        text-align: center;
        border: 1px solid rgba(255, 255, 255, 0.5);
        background-color: rgba(0, 0, 0, 0.3);
        padding: 1rem;
        z-index: 2;
        border-radius: 0.4rem;

        h5 {
            font-size: 1.3rem;
            color: #fff;
            line-height: 1.6rem;
            margin-bottom: 1rem;
        }
    }
}
</style>