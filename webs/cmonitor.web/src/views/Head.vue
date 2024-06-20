<template>
    <div>
        <el-dialog class="options-center" title="选择角色" destroy-on-close v-model="showSelectUsername" center
            :show-close="false" :close-on-click-modal="false" align-center width="70%">
            <div class="username-wrap t-c">
                <el-select filterable allow-create default-first-option v-model="state.username" @change="handleChange"
                    placeholder="选择角色" size="large">
                    <el-option v-for="item in state.usernames" :key="item" :label="item" :value="item" />
                </el-select>
            </div>
            <template #footer>
                <el-button type="success" @click="handleUsername" plain>确 定</el-button>
            </template>
        </el-dialog>
        <el-dialog class="options-center" title="管理接口" destroy-on-close v-model="showPort" center :show-close="false"
            :close-on-click-modal="false" align-center width="70%">
            <div class="port-wrap t-c">
                <div>
                    接口 : <el-input v-model="state.api" style="width:70%"></el-input>
                </div>
                <div style="padding-top:1rem ;">
                    秘钥 : <el-input type="password" v-model="state.psd" style="width:70%"></el-input>
                </div>
                <div style="padding-top:1rem ;">
                    分组 : <el-input v-model="state.groupid" style="width:70%"></el-input>
                </div>
            </div>
            <template #footer>
                <el-button type="success" @click="handleConnect" plain>确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { computed, onMounted, reactive, watch } from 'vue';
import { initWebsocket, subWebsocketState,closeWebsocket } from '../apis/request'
import { getRules } from '../apis/rule'
import { getConfig } from '../apis/signin'
import { useRoute, useRouter } from 'vue-router';
import { injectGlobalData } from './provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();
        const router = useRouter();
        const queryCache = JSON.parse(localStorage.getItem('api-cache') || JSON.stringify({username:'',api:`${window.location.hostname}:1801`,psd:'snltty',groupid:'snltty'}));
        
        const state = reactive({
            api:queryCache.api,
            psd:queryCache.psd,
            groupid: globalData.value.groupid || queryCache.groupid,
            usernames: [],
            username: globalData.value.username || queryCache.username,
            showPort: false
        });

        const showSelectUsername = computed(() => !!!globalData.value.username && globalData.value.connected);
        const showPort = computed(() => globalData.value.connected == false && state.showPort);

        watch(() => globalData.value.updateRuleFlag, () => {
            _getRules();
            _getConfig();
        });
        watch(() => globalData.value.updateDeviceFlag, () => {
            _getRules();
            _getConfig();
        });

        const _getConfig = ()=>{
            getConfig().then((res)=>{
                globalData.value.config.Common = res.Data.Common;
                globalData.value.config.Server = res.Data.Server;
            }).catch((err)=>{});
        }

        const _getRules = () => {
            getRules().then((res) => {
                for (let j in res.Data) {
                    for (let jj in res.Data[j]) {
                        res.Data[j][jj] = JSON.parse(res.Data[j][jj]);
                    }
                }
                globalData.value.usernames = res.Data;
                globalData.value.usernames[globalData.value.username] = globalData.value.usernames[globalData.value.username] || {}
                state.usernames = Object.keys(res.Data);
            }).catch(() => { });
        }

        const saveCache = ()=>{
            globalData.value.username = state.username;
            globalData.value.groupid = state.groupid;
            queryCache.api = state.api;
            queryCache.psd = state.psd;
            queryCache.groupid = state.groupid;
            queryCache.username = state.username;
            localStorage.setItem('api-cache',JSON.stringify(queryCache));
        }
        const handleConnect = () => {
            saveCache();
            closeWebsocket();
            //initWebsocket(`ws://192.168.1.18:1801`,state.psd);
            initWebsocket(`ws://${state.api}`,state.psd);
        }
        const handleUsername = () => {
            saveCache();
            document.title = `班长-${globalData.value.username}`
        }

        onMounted(() => {
            
            _getRules();

            _getConfig();

            setTimeout(() => { state.showPort = true; }, 100);

            subWebsocketState((state) => { if (state) globalData.value.updateRuleFlag = Date.now(); });

            router.isReady().then(()=>{        
                state.api = route.query.api ?`${window.location.hostname}:${route.query.api}` :  state.api;
                state.psd = route.query.psd || state.psd;
                state.groupid = route.query.groupid || state.groupid;
                state.username = route.query.username || state.username;
                handleUsername();
                handleConnect();
            });
        });

        return {
            state, showSelectUsername, showPort, handleUsername, handleConnect, handleChange
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>