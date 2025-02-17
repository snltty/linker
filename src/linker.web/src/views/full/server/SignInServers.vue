<template>
    <div class="signin-wrap" :style="{height:`${state.height}px`}">
        <el-card shadow="never">
            <template #header>{{$t('server.messenger')}}</template>
            <div>
                <el-form label-width="auto">
                    <el-form-item :label="$t('server.messengerAddr')">
                        <div class="flex">
                            <el-input class="flex-1" v-model="state.list.Host" @change="handleSave" />
                            <span>{{$t('server.messengerText')}}</span>
                        </div>
                    </el-form-item>
                    <el-form-item :label="$t('server.messengerSecretKey')">
                        <div class="flex">
                            <el-input class="flex-1" type="password" show-password maxlength="36" v-model="state.list.SecretKey" @change="handleSave" />
                            <span>{{$t('server.messengerSecretKeyText')}}</span>
                        </div>
                    </el-form-item>
                    <RelayServers></RelayServers>
                    <SForward></SForward>
                    <Updater></Updater>
                </el-form>
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
import { setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, reactive } from 'vue'
import SForward from './SForward.vue';
import Updater from './Updater.vue';
import RelayServers from './RelayServers.vue';
import { useI18n } from 'vue-i18n';
export default {
    components:{SForward,Updater,RelayServers},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Server,
            height: computed(()=>globalData.value.height-90),
        });

        const handleSave = ()=>{
            setSignInServers(state.list).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
        }

        return {state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>