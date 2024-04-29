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
                    秘钥 : <el-input type="password" v-model="state.apipsd" style="width:70%"></el-input>
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
import { getRules, addName } from '../apis/rule'
import { getConfig } from '../apis/signin'
import { useRoute } from 'vue-router';
import { injectGlobalData } from './provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();

        const state = reactive({
            api: route.query.api ? `${window.location.hostname}:${route.query.api}` : (localStorage.getItem('api') || `${window.location.hostname}:1801`),
            apipsd: route.query.apipsd ? `${route.query.apipsd}` : (localStorage.getItem('apipsd') || `snltty`),
            groupid: route.query.groupid ? `${route.query.groupid}` : (localStorage.getItem('groupid') || `snltty`),
            usernames: [],
            username: globalData.value.username || localStorage.getItem('username') || '',
            showPort: false
        });
        localStorage.setItem('api', state.api);
        localStorage.setItem('apipsd', state.apipsd);
        localStorage.setItem('groupid', state.groupid);
        globalData.value.username = state.username;
        globalData.value.groupid = state.groupid;

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
                state.usernames = Object.keys(res.Data);
            }).catch(() => { });
        }
        const handleConnect = () => {
            closeWebsocket();
            //initWebsocket(`ws://hk.cmonitor.snltty.com:1801`,state.apipsd);
            initWebsocket(`ws://${state.api}`,state.apipsd);
            localStorage.setItem('api', state.api);
            localStorage.setItem('apipsd', state.apipsd);
            localStorage.setItem('groupid', state.groupid);
        }
        const handleUsername = () => {
            globalData.value.username = state.username || '';
            globalData.value.groupid = state.groupid || '';
            localStorage.setItem('username', globalData.value.username);
            //localStorage.setItem('groupid', globalData.value.groupid);
            //localStorage.setItem('apipsd', globalData.value.apipsd);
            document.title = `班长-${globalData.value.username}`
        }
        const handleChange = (value) => {
            addName(value).then(() => {
                globalData.value.updateRuleFlag = Date.now();
            }).catch(() => {
                globalData.value.updateRuleFlag = Date.now();
            })
        }

        onMounted(() => {
            handleUsername();
            handleConnect();
            _getRules();

            _getConfig();

            setTimeout(() => { state.showPort = true; }, 100);

            subWebsocketState((state) => { if (state) globalData.value.updateRuleFlag = Date.now(); });
        });

        return {
            state, showSelectUsername, showPort, handleUsername, handleConnect, handleChange
        }
    }
}
</script>

<style lang="stylus" scoped>
</style>