<template>
    <div class="signin-wrap h-100">
        <el-card shadow="never" class="h-100 flex flex-column flex-nowrap">
            <template #header>
                <div class="flex">
                    <span>{{$t('messenger')}}</span>
                    <span class="flex-1"></span>
                    <Export></Export>
                </div>
            </template>
            <div class="absolute scrollbar">
                <el-form label-width="auto" :label-position="state.position">
                    <el-form-item :label="$t('messenger.addr')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.list.Host==state.signinHost}" class="w-20" v-model="state.list.Host" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInServer"></Sync>
                            <PcShow>
                                <span class="mgl-1">{{$t('messenger.alert')}}</span>
                            </PcShow>
                        </div>
                    </el-form-item>
                    <el-form-item :label="`${$t('messenger.addr')}1`">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.list.Host1==state.signinHost}" class="w-20" v-model="state.list.Host1" @blur="handleSave" />
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>
                    <el-form-item :label="$t('messenger.super.key')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.super,error:state.super==false}" class="w-20" type="password" show-password maxlength="36" v-model="state.list.SuperKey" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInSuperKey"></Sync>
                        </div>
                    </el-form-item>
                    <el-form-item :label="$t('messenger.super.password')">
                        <div class="flex">
                            <el-input v-trim :class="{success:state.super,error:state.super==false}" class="w-20" type="password" show-password maxlength="36" v-model="state.list.SuperPassword" @blur="handleSave" />
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>   
                    <el-form-item :label="$t('messenger.userid')">
                        <div class="flex">
                            <el-input v-trim class="w-20" type="password" show-password maxlength="36" v-model="state.list.UserId" @blur="handleSave" />
                            <span class="mgl-1"></span>
                            <Sync name="SignInUserId"></Sync>
                            <PcShow>
                                <span class="mgl-1">{{$t('messenger.userid.alert')}}</span>
                            </PcShow>
                        </div>
                    </el-form-item>
                    <el-form-item></el-form-item>
                    <RelayServers class="mgt-2"></RelayServers>
                    <ReverseServers class="mgt-2"></ReverseServers>
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
import Updater from '../updater/Config.vue';
import RelayServers from '../relay/Config.vue';
import ReverseServers from '../forward/Config.vue';
import { useI18n } from 'vue-i18n';
import Sync from '../sync/Index.vue'
import WhiteList from '../wlist/Index.vue';
import Export from './Export.vue';
export default {
    components:{Updater,RelayServers,ReverseServers,Sync,WhiteList,Export},
    setup(props) {
        const {t} = useI18n();
        const globalData = injectGlobalData();
        const state = reactive({
            list:globalData.value.config.Client.Server,
            position: computed(()=>globalData.value.isPhone ? 'top':'right'),
            super:computed(()=>globalData.value.signin.Super),
            signinHost:computed(()=>globalData.value.signin.SignInHost),
        });

        const handleSave = ()=>{
            setSignInServers(state.list).then(()=>{
                ElMessage.success(t('common.opered'));
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
.scrollbar{
    padding:var(--el-card-padding);
}
</style>