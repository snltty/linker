<template>
    <el-form-item :label="$t('server.updater')">
        <div class="flex">
            <el-checkbox v-model="state.sync2Server" @change="handleSync2ServerChange">{{ $t('server.updaterSync2Server') }}</el-checkbox>
            <Sync class="mgl-1" name="UpdaterSecretKey"></Sync>
        </div>
    </el-form-item>
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
                ElMessage.success(t('common.oper'));
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