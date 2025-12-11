<template>
    <div class="signin-wrap" :style="{height:`${state.height}px`}">
        <el-card shadow="never">
            <template #header>
                <div class="flex">
                    <span>{{$t('server.messenger')}}</span>
                    <span class="flex-1"></span>
                    <Export></Export>
                </div>
            </template>
            <div>
                <el-form label-width="auto" :label-position="state.position">
                    <el-form-item :label="$t('server.messengerAddr')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.list.Host==state.signinHost}" style="width:20rem;" v-model="state.list.Host" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInServer"></Sync>
                            <PcShow>
                                <span class="mgl-1">{{$t('server.messengerText')}}</span>
                            </PcShow>
                        </div>
                    </el-form-item>
                    <el-form-item :label="`${$t('server.messengerAddr')}1`">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.list.Host1==state.signinHost}" style="width:20rem;" v-model="state.list.Host1" @blur="handleSave" />
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>
                    <el-form-item :label="$t('server.messengerSuperKey')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.super,error:state.super==false}" style="width:20rem;" type="password" show-password maxlength="36" v-model="state.list.SuperKey" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInSuperKey"></Sync>
                        </div>
                    </el-form-item>
                    <el-form-item :label="$t('server.messengerSuperPassword')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.super,error:state.super==false}" style="width:20rem;" type="password" show-password maxlength="36" v-model="state.list.SuperPassword" @blur="handleSave" />
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>   
                    <el-form-item :label="$t('server.messengerUserId')">
                        <div class="flex">
                            <el-input v-trim style="width:20rem;" type="password" show-password maxlength="36" v-model="state.list.UserId" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInUserId"></Sync>
                            <PcShow>
                                <span class="mgl-1">{{$t('server.messengerUserIdText')}}</span>
                            </PcShow>
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>
                    <RelayServers class="mgt-2"></RelayServers>
                    <SForwardServers class="mgt-2"></SForwardServers>
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
import { checkSignInKey, setSignInServers } from '@/apis/signin';
import { injectGlobalData } from '@/provide';
import { ElMessage } from 'element-plus';
import { computed, onMounted, reactive } from 'vue'
import Updater from '../../../components/updater/Config.vue';
import RelayServers from '../../../components/relay/Config.vue';
import SForwardServers from '../../../components/forward/Config.vue';
import { useI18n } from 'vue-i18n';
import Sync from '../../../components/sync/Index.vue'
import WhiteList from '../../../components/wlist/Index.vue';
import Export from './Export.vue';
export default {
    components:{Updater,RelayServers,SForwardServers,Sync,WhiteList,Export},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Server,
            height: computed(()=>globalData.value.height-90),
            position: computed(()=>globalData.value.isPhone ? 'top':'right'),
            super:computed(()=>globalData.value.signin.Super),
            signinHost:computed(()=>globalData.value.signin.SignInHost),
        });

        const handleSave = ()=>{
            setSignInServers(state.list).then(()=>{
                ElMessage.success(t('common.oper'));
            }).catch((err)=>{
                console.log(err);
                ElMessage.error(t('common.operFail'));
            });
            handleCheckKey();
        }
        const handleCheckKey = ()=>{
            checkSignInKey().then((res)=>{}).catch(()=>{});
        }

        onMounted(()=>{
            handleCheckKey();
        });

        return {globalData,state,handleSave}
    }
}
</script>
<style lang="stylus" scoped>
    
</style>