<template>
    <div>
        <div class="head flex">
            <div class="logo">
                <router-link :to="{name:'Index'}">
                    <img src="../assets/logo.png" alt="">
                </router-link>
            </div>
            <div class="menu flex-1">
                <ul class="flex">
                    <li>
                        <router-link :to="{name:'Index'}">首页</router-link>
                    </li>
                    <li>
                        <router-link :to="{name:'Settings'}">配置</router-link>
                    </li>
                    <li>
                        <router-link :to="{name:'Logger'}">日志</router-link>
                    </li>
                </ul>
            </div>
        </div>
        <el-dialog class="options-center" title="管理接口" destroy-on-close v-model="showPort" center :show-close="false"
            :close-on-click-modal="false" align-center width="200">
            <div class="port-wrap t-c">
                <div>
                   接口 : <el-input v-model="state.api" style="width:70%"></el-input>
                </div>
                <div class="pdt-10">
                   秘钥 : <el-input type="password" v-model="state.psd" style="width:70%"></el-input>
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
import { getConfig,getSignInfo } from '../apis/signin'
import { useRoute, useRouter } from 'vue-router';
import { injectGlobalData } from '../provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();
        const router = useRouter();

        const queryCache = JSON.parse(localStorage.getItem('api-cache') || JSON.stringify({api:`${window.location.hostname}:1803`,psd:'snltty',groupid:'snltty'}));
        const state = reactive({
            api:queryCache.api,
            psd:queryCache.psd,
            groupid:globalData.value.groupid || queryCache.groupid,
            showPort: false
        });
        const showPort = computed(() => globalData.value.connected == false && state.showPort);

        const handleConnect = () => {
            globalData.value.groupid = state.groupid;
            queryCache.api = state.api;
            queryCache.psd = state.psd;
            queryCache.groupid = state.groupid;
            localStorage.setItem('api-cache',JSON.stringify(queryCache));
            closeWebsocket();
            //initWebsocket(`ws://192.168.1.18:1803`,state.psd);
            initWebsocket(`ws://${state.api}`,state.psd);
        }

        const _getConfig = ()=>{
            getConfig().then((res)=>{
                globalData.value.config.Common = res.Common;
                globalData.value.config.Client = res.Client;
                globalData.value.config.Running = res.Running;
                globalData.value.configed = true;
                setTimeout(()=>{
                    _getConfig();
                },1000);
            }).catch((err)=>{
                setTimeout(()=>{
                    _getConfig();
                },1000);
            });
        }
        const _getSignInfoInfo = ()=>{
            getSignInfo().then((res)=>{
                globalData.value.signin.Connected = res.Connected;
                globalData.value.signin.Connecting = res.Connecting;
                setTimeout(()=>{
                    _getSignInfoInfo();
                },1000);
            }).catch((err)=>{
                setTimeout(()=>{
                    _getSignInfoInfo();
                },1000);
            });
        }

        onMounted(() => {
            setTimeout(() => { state.showPort = true; }, 100);
            subWebsocketState((state) => { if (state) {
                _getConfig();
                _getSignInfoInfo();
            }});
            router.isReady().then(()=>{
                state.api = route.query.api ?`${window.location.hostname}:${route.query.api}` :  state.api;
                state.psd = route.query.psd || state.psd;
                state.groupid = route.query.groupid || state.groupid;
                handleConnect();
            });
        });

        return {
            state,  showPort,  handleConnect
        }
    }
}
</script>

<style lang="stylus" scoped>
.head{
    background-color:#f6f8fa;
    border-bottom:1px solid #d0d7de;
    box-shadow:1px 1px 4px rgba(0,0,0,0.05);
    height:5rem;
    line-height:5rem;
    .logo{
        padding:.5rem 0 0 1rem;
        img{vertical-align:top;height:4rem;}
    }
    .menu{
        padding-left:1rem;font-size:1.4rem;
        li{box-sizing:border-box;padding:.5rem 0;margin-right:.5rem;}
        a{
            display:block;
            color:#333;
            padding:0 1rem;
            line-height:4rem
            &:hover,&.router-link-active{
                background-color:rgba(0,0,0,0.1);
                font-weight:bold;
            }
        }
    }
}

</style>