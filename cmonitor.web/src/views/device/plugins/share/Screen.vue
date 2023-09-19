<template>
    <div class="share-lock-wrap" v-if="data.Share.Lock.Value.val == 'ask'">
        <div class="inner">
            <h3>请求解锁</h3>
            <h5>
                <span>【{{data.Share.Lock.TypeText}}】</span>
                <span v-if="data.Share.Lock.Value.type == 'remark-block'">【{{data.Share.Lock.Value.star}}】星</span>
                <span v-if="data.Share.Lock.Value.type == 'remark-cpp'">【{{data.Share.Lock.Value.star}}】星</span>
            </h5>
            <div>
                <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="确认驳回请求吗?" @confirm="handleReject">
                    <template #reference>
                        <el-button :loading="state.loading" type="danger" plain round size="small">驳回</el-button>
                    </template>
                </el-popconfirm>
                <el-popconfirm confirm-button-text="确认" cancel-button-text="取消" title="确认通过请求吗?" @confirm="handleConfirm">
                    <template #reference>
                        <el-button :loading="state.loading" type="success" plain round size="small">确认</el-button>
                    </template>
                </el-popconfirm>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive } from 'vue'
import { shareUpdate } from '@/apis/share'
import { ElMessage } from 'element-plus';
export default {
    props: ['data'],
    setup(props) {

        const state = reactive({
            loading: false
        });
        const handleReject = () => {
            state.loading = true;
            let value = JSON.parse(JSON.stringify(props.data.Share.Lock.Value));
            value.val = 'reject';
            shareUpdate(props.data.MachineName, {
                index: props.data.Share.Lock.Index,
                value: JSON.stringify(value)
            }).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功！');
                } else {
                    ElMessage.error('操作失败！');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败！');
            });
        }
        const handleConfirm = () => {
            state.loading = true;
            let value = JSON.parse(JSON.stringify(props.data.Share.Lock.Value));
            value.val = 'confirm';
            shareUpdate(props.data.MachineName, {
                index: props.data.Share.Lock.Index,
                value: JSON.stringify(value)
            }).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功！');
                } else {
                    ElMessage.error('操作失败！');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败！');
            });
        }

        return {
            data: props.data, state, handleReject, handleConfirm
        }
    }
}
</script>

<style lang="stylus" scoped>
.share-lock-wrap {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;

    // background-color: rgba(0, 0, 0, 0.2);
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

        h3 {
            font-size: 1.5rem;
            font-weight: 600;
            color: #fff;
        }

        h5 {
            font-size: 1.3rem;
            padding-bottom: 1rem;
            color: #fff;
        }
    }
}
</style>