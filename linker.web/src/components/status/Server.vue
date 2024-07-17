<template>
    <div class="status-server-wrap" :class="{ connected: state.connected }">
        <a href="javascript:;" @click="handleConfig">
            <el-icon size="16"><Promotion /></el-icon>
            <template v-if="state.connected">信标服务器 {{state.version}}</template>
            <template v-else>信标服务器</template>
        </a>
    </div>
    <el-dialog v-model="state.show" title="连接设置" width="300">
        <div>
            <el-form :model="state.form" :rules="state.rules" label-width="6rem">
                <el-form-item label="机器名" prop="name">
                    <el-input v-model="state.form.name" maxlength="12" show-word-limit />
                </el-form-item>
                <el-form-item label="分组名" prop="groupid">
                    <el-input v-model="state.form.groupid" maxlength="36" show-word-limit />
                </el-form-item>
            </el-form>
        </div>
        <template #footer>
            <div class="dialog-footer t-c">
                <el-button @click="state.show = false" :loading="state.loading">取消</el-button>
                <el-button type="primary" @click="handleSave" :loading="state.loading">确定保存</el-button>
            </div>
        </template>
    </el-dialog>
</template>
<script>
import { setSignIn } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue';
import {Promotion} from '@element-plus/icons-vue'
export default {
    components:{Promotion},
    setup(props) {

        const globalData = injectGlobalData();

        const state = reactive({
            show: false,
            loading: false,
            connected: computed(() => globalData.value.signin.Connected),
            connecting: computed(() => globalData.value.signin.Connecting),
            version: computed(() => globalData.value.signin.Version),
            server: computed(() => globalData.value.config.Client.Server),
            serverLength: computed(() => (globalData.value.config.Running.Client.Servers || []).length),
            form: {
                name: globalData.value.config.Client.Name,
                groupid: globalData.value.config.Client.GroupId,
            },
            rules: {},
        });

        const handleConfig = () => {
            state.form.name = globalData.value.config.Client.Name;
            state.form.groupid = globalData.value.config.Client.GroupId;
            state.show = true;
        }
        const handleSave = () => {
            state.loading = true;
            setSignIn(state.form).then(() => {
                state.loading = false;
                state.show = false;
                ElMessage.success('已操作');
            }).catch((err) => {
                state.loading = false;
                ElMessage.success('操作失败!');
            });
        }

        return {
            state, handleConfig, handleSave,

        }
    }
}
</script>
<style lang="stylus" scoped>
.status-server-wrap{
    padding-right:.5rem;
    a{color:#333;}
    span{border-radius:1rem;background-color:rgba(0,0,0,0.1);padding:0 .6rem; margin-left:.2rem}

    &.connected {
       a{color:green;font-weight:bold;}
    }  

    .el-icon{
        vertical-align:text-bottom;
    }
}

</style>