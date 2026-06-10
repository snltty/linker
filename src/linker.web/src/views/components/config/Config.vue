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
                <el-descriptions size="small" title="" :column="1" border :direction="state.direction">
                    <el-descriptions-item label-width="15rem">
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('messenger.addr') }}<span class="flex-1"></span><Sync name="SignInServer"></Sync></div>
                        </template>
                        <el-input class="w-auto" v-trim :class="{success:state.list.Host==state.signinHost}" v-model="state.list.Host" @blur="handleSave" />
                    </el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('messenger.addr') }}1</div>
                        </template>
                        <el-input class="w-auto" v-trim :class="{success:state.list.Host1==state.signinHost}" v-model="state.list.Host1" @blur="handleSave" />
                    </el-descriptions-item>
                    <el-descriptions-item></el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('messenger.super.key') }}<span class="flex-1"></span><Sync name="SignInSuperKey"></Sync></div>
                        </template>
                        <el-input class="w-auto" v-trim :class="{success:state.super,error:state.super==false}" type="password" show-password maxlength="36" v-model="state.list.SuperKey" @blur="handleSave" />
                    </el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('messenger.super.password') }}</div>
                        </template>
                        <el-input class="w-auto" v-trim :class="{success:state.super,error:state.super==false}" type="password" show-password maxlength="36" v-model="state.list.SuperPassword" @blur="handleSave" />
                    </el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('messenger.userid') }}<span class="flex-1"></span><Sync name="SignInUserId"></Sync></div>
                        </template>
                        <el-input class="w-auto" v-trim type="password" show-password maxlength="36" v-model="state.list.UserId" @blur="handleSave" />
                    </el-descriptions-item>
                    <el-descriptions-item></el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('relay') }}</div>
                        </template>
                        <RelayServers></RelayServers>
                    </el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('reverse') }}</div>
                        </template>
                        <ReverseServers></ReverseServers>
                    </el-descriptions-item>
                    <el-descriptions-item></el-descriptions-item>
                    <el-descriptions-item>
                        <template #label>
                            <div class="flex flex-items-center">{{ $t('updater') }}<span class="flex-1"></span><Sync class="mgl-1" name="UpdaterSecretKey"></Sync></div>
                        </template>
                        <Updater></Updater>
                    </el-descriptions-item>
                </el-descriptions>
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
            direction: computed(()=>globalData.value.isPhone ? 'vertical':'horizontal'),
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