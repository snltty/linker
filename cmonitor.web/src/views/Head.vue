<template>
    <div>
        <el-dialog title="选择角色" destroy-on-close v-model="showSelectUsername" center :show-close="false" :close-on-click-modal="false" align-center width="70%">
            <div class="username-wrap t-c">
                <el-select filterable allow-create default-first-option v-model="state.username" @change="handleChange" placeholder="选择角色" size="large">
                    <el-option v-for="item in state.usernames" :key="item" :label="item" :value="item" />
                </el-select>
            </div>
            <template #footer>
                <el-button type="primary" @click="handleUsername">确 定</el-button>
            </template>
        </el-dialog>
        <el-dialog title="管理端口" destroy-on-close v-model="showPort" center :show-close="false" :close-on-click-modal="false" align-center width="70%">
            <div class="port-wrap t-c">
                <el-input v-model="state.port" style="width:auto"></el-input>
            </div>
            <template #footer>
                <el-button type="primary" @click="handleConnect">确 定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script>
import { computed, onMounted, reactive, watch } from 'vue';
import { initWebsocket } from '../apis/request'
import { getRules, addName } from '../apis/hijack'
import { useRoute } from 'vue-router';
import { injectGlobalData } from './provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();
        const port = +(route.query.api || localStorage.getItem('port') || 1801);
        localStorage.setItem('port', port);

        globalData.value.username = globalData.value.username || localStorage.getItem('username') || '';
        const state = reactive({
            port: port,
            usernames: [],
            username: globalData.value.username,
            showPort: false
        });
        const showSelectUsername = computed(() => !!!globalData.value.username && globalData.value.connected);
        const showPort = computed(() => globalData.value.connected == false && state.showPort);

        watch(() => globalData.value.updateFlag, () => {
            _getRules();
        });

        const _getRules = () => {
            getRules().then((res) => {
                globalData.value.usernames = res;
                state.usernames = Object.keys(res);
            }).catch(() => { });
        }
        onMounted(() => {
            handleUsername();
            handleConnect();
            _getRules();

            setTimeout(() => {
                state.showPort = true;
            }, 100);
        });

        const handleConnect = () => {
            initWebsocket(`ws://${window.location.hostname}:${state.port}`);
            localStorage.setItem('port', state.port);
        }
        const handleUsername = () => {
            globalData.value.username = state.username || '';
            localStorage.setItem('username', globalData.value.username);
            document.title = `班长-${globalData.value.username}`
        }
        const handleChange = (value) => {
            addName(value).then(() => {
                globalData.value.updateFlag = Date.now();
            }).catch(() => {
                globalData.value.updateFlag = Date.now();
            })
        }

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