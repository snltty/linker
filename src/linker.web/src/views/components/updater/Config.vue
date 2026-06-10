<template>
    <div class="flex flex-items-center">
        <el-checkbox v-model="state.sync2Server" @change="handleSync2ServerChange">{{ $t('updater.sync2Server') }}</el-checkbox>
    </div>
</template>
<script>
import { setSync2Server } from '@/apis/updater';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import {  reactive } from 'vue'
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
export default {
    components:{Sync},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            sync2Server:globalData.value.config.Client.Updater.Sync2Server,
        });
        const handleSync2ServerChange = ()=>{
            setSync2Server(state.sync2Server).then(()=>{
                ElMessage.success(t('common.opered'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        return {globalData,state,handleSync2ServerChange}
    }
}
</script>
<style lang="stylus" scoped>

</style>