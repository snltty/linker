<template>
    <div>
        <el-dialog class="options-center" title="选择角色" destroy-on-close v-model="showSelectUsername" center :show-close="false" :close-on-click-modal="false" align-center width="70%">
            <div class="username-wrap t-c">
                <el-select filterable allow-create default-first-option v-model="state.username" @change="handleChange" placeholder="选择角色" size="large">
                    <el-option v-for="item in state.usernames" :key="item" :label="item" :value="item" />
                </el-select>
            </div>
            <template #footer>
                <el-button type="success" @click="handleUsername" plain>确 定</el-button>
            </template>
        </el-dialog>
        <el-dialog class="options-center" title="管理接口" destroy-on-close v-model="showPort" center :show-close="false" :close-on-click-modal="false" align-center width="70%">
            <div class="port-wrap t-c">
                <el-input v-model="state.api" style="width:auto"></el-input>
            </div>
            <template #footer>
                <el-button type="success" @click="handleConnect" plain>确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { computed, onMounted, reactive, watch } from 'vue';
import { initWebsocket, subWebsocketState } from '../apis/request'
import { getRules, addName } from '../apis/rule'
import { useRoute } from 'vue-router';
import { injectGlobalData } from './provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();

        const state = reactive({
            api: route.query.api ? `${window.location.hostname}:${route.query.api}` : (localStorage.getItem('api') || `${window.location.hostname}:1801`),
            usernames: [],
            username: globalData.value.username || localStorage.getItem('username') || '',
            showPort: false
        });
        localStorage.setItem('api', state.api);
        globalData.value.username = state.username;

        const showSelectUsername = computed(() => !!!globalData.value.username && globalData.value.connected);
        const showPort = computed(() => globalData.value.connected == false && state.showPort);

        watch(() => globalData.value.updateRuleFlag, () => {
            _getRules();
        });
        watch(() => globalData.value.updateDeviceFlag, () => {
            _getRules();
        });


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
            //  initWebsocket(`ws://192.168.1.18:1801`);
            initWebsocket(`ws://${state.api}`);
            localStorage.setItem('api', state.api);
        }
        const handleUsername = () => {
            globalData.value.username = state.username || '';
            localStorage.setItem('username', globalData.value.username);
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
.head-wrap {
    text-align: center;
    padding: 0.5rem 0;
    line-height: 4rem;
    border-bottom: 1px solid #ddd;
    background-color: #f0f0f0;
    font-size: 1.5rem;
    font-weight: bold;
    z-index: 999;
    position: relative;
    box-shadow: 1px 1px 4px rgba(0, 0, 0, 0.075);
}

img {
    height: 4rem;
    vertical-align: middle;
    margin-right: 0.6rem;
}
</style>