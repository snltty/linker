<template>
    <el-card shadow="never" class="h-100 flex flex-column flex-nowrap">
        <template #header>
            <div class="flex">
                <span class="flex-1">{{$t('action.text')}}</span>
                <Sync name="ActionStatic" v-if="state.isSelf"></Sync>
            </div>
        </template>
        <div class="absolute scrollbar">
            <el-input v-trim v-model="state.data" type="textarea" resize="none" @change="handleSave" class="h-100" />
        </div>
        <template #footer>
            <div class="t-c">
                <el-button type="success" @click="handleSave">{{$t('common.confirm')}}</el-button>
            </div>
        </template>
    </el-card>
</template>
<script>
import { getArgs, setArgs } from '@/apis/action';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import Sync from '../sync/Index.vue'
export default {
    props:['machineId'],
    components: { Sync },
    setup(props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            data: '',
            machineId: props.machineId || globalData.value.config.Client.Id,
            isSelf:computed(()=>{
                return state.machineId == globalData.value.config.Client.Id;
            })
        });
        const loadData = () => { 
            getArgs(state.machineId).then((res) => {
                state.data = res;
            });
        }
        const handleSave = () => {
            try {
                if (state.data && typeof (JSON.parse(state.data)) != 'object') {
                    ElMessage.error(t('action.jsonError'));
                    return;
                }
            } catch (e) {
                ElMessage.error(t('action.jsonError'));
                return;
            }
            setArgs({
                Key: state.machineId,
                Value: state.data
            }).then(() => {
                ElMessage.success(t('common.oper'));
            }).catch((err) => {
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }
        onMounted(()=>{
            loadData();
        })

        return { state, handleSave }
    }
}
</script>
<style lang="stylus" scoped>
.scrollbar{
    padding:var(--el-card-padding);
}
</style>