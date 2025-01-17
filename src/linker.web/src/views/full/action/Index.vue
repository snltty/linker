<template>
    <div class="action-wrap">
        <el-card shadow="never">
            <template #header>{{$t('action.text')}}</template>
            <div>
                <el-input v-model="state.list" :rows="10" type="textarea" resize="none" @change="handleSave" />
            </div>
            <template #footer>
                <div class="t-c">
                    <el-button type="success" @click="handleSave">{{$t('common.confirm')}}</el-button>
                </div>
            </template>
        </el-card>
    </div>
</template>
<script>
import { setArgs } from '@/apis/action';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { reactive } from 'vue'
import { useI18n } from 'vue-i18n'
export default {
    setup(props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list: globalData.value.config.Client.Action.Args[globalData.value.config.Client.Server.Host] || ''
        });
        const handleSave = () => {
            try {
                if (state.list && typeof (JSON.parse(state.list)) != 'object') {
                    ElMessage.error(t('action.jsonError'));
                    return;
                }
            } catch (e) {
                ElMessage.error(t('action.jsonError'));
                return;
            }
            const json = JSON.parse(JSON.stringify(globalData.value.config.Client.Action.Args));
            json[globalData.value.config.Client.Server.Host] = state.list;
            setArgs(json).then(() => {
                ElMessage.success(t('common.oper'));
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });;
        }

        return { state, handleSave }
    }
}
</script>
<style lang="stylus" scoped>
.action-wrap{
    font-size:1.3rem;
    padding:1.5rem
}
</style>