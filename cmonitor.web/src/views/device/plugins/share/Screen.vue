<template>
    <div class="share-lock-wrap" v-if="data.Share.Lock.Value.val == 'ask'">
        <div class="inner">
            <h5>
                <span>【{{data.Share.Lock.TypeText}}】</span>
                <template v-if="data.Share.Lock.Value.type == 'remark-block' || data.Share.Lock.Value.type == 'remark-cpp'">
                    <el-checkbox v-model="data.Share.Lock.Value.notify" size="small">广播</el-checkbox>
                </template>
            </h5>
            <div>
                <template v-if="data.Share.Lock.Value.type == 'remark-block' || data.Share.Lock.Value.type == 'remark-cpp'">
                    <el-rate @change="handleStarChange" v-model="data.Share.Lock.Value.star" size="large" />
                </template>
            </div>
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
import { notifyUpdate } from '@/apis/notify'
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
            props.data.Share.Lock.Value.notify = false;
            shareUpdate(props.data.MachineName, {
                index: props.data.Share.Lock.Index,
                value: JSON.stringify(value)
            }).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功！');
                    if (value.notify) {
                        notifyUpdate(2, props.data.Share.UserName.Value, value.star);
                    }
                } else {
                    ElMessage.error('操作失败！');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败！');
            });
        }

        const handleStarChange = () => {
            state.loading = true;
            let value = JSON.parse(JSON.stringify(props.data.Share.Lock.Value));
            shareUpdate(props.data.MachineName, {
                index: props.data.Share.Lock.Index,
                value: JSON.stringify(value)
            }).then((res) => {
                state.loading = false;
            }).catch(() => {
                state.loading = false;
                ElMessage.error('操作失败！');
            });
        }

        return {
            data: props.data, state, handleReject, handleConfirm, handleStarChange
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

            .el-checkbox {
                height: 1.6rem;
                vertical-align: bottom;
                color: #fff;
            }
        }

        .el-rate.el-rate--large {
            height: inherit;
            padding: 0.6rem 0 1.6rem 0;
        }
    }
}
</style>