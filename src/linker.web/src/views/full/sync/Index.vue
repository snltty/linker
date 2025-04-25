<template>
    <div>
        <el-button class="btn" size="small" @click="handleSync"><el-icon><Share /></el-icon></el-button>
    </div>
</template>

<script>
import { setSync } from '@/apis/sync';
import { injectGlobalData } from '@/provide';
import {Share} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
export default {
    props:['name'],
    components: {Share},
    setup (props) {
        const { t } = useI18n();
        const globalData = injectGlobalData();
        const hasSync = computed(()=>globalData.value.hasAccess('Sync'));
        const handleSync = ()=>{
            if(!hasSync.value){
                ElMessage.success(t('common.access'));
                return;
            }
            ElMessageBox.confirm(`${t('server.sync')}【${t(`server.async${props.name}`)}】${t(`server.asyncText`)}? `, t('common.tips'), {
                confirmButtonText: t('common.confirm'),
                cancelButtonText:t('common.cancel'),
                type: 'warning'
            }).then(() => {
                setSync([props.name]).then(res => {
                    ElMessage.success(t('common.oper'));
                });
            }).catch(() => {});

        }
        return {
            handleSync
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>