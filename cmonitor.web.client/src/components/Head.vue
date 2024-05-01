<template>
    <div>
        <div class="head flex">
            <div class="logo">
                <router-link :to="{name:'Index'}">
                    <img src="../assets/logo.png" alt="">
                </router-link>
            </div>
            <div class="menu">
                <ul class="flex-1">
                    <li>
                        <router-link :to="{name:'Index'}">首页</router-link>
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
                   秘钥 : <el-input type="password" v-model="state.apipsd" style="width:70%"></el-input>
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
import { initWebsocket, subWebsocketState } from '../apis/request'
import { getConfig,getSignInfo } from '../apis/tunnel'
import { useRoute } from 'vue-router';
import { injectGlobalData } from '../provide';
export default {
    setup() {

        const globalData = injectGlobalData();
        const route = useRoute();

        const state = reactive({
            api: route.query.api ? `${window.location.hostname}:${route.query.api}` : (localStorage.getItem('api') || `${window.location.hostname}:1805`),
            apipsd: route.query.apipsd ? `${route.query.apipsd}` : (localStorage.getItem('apipsd') || `snltty`),
            groupid: route.query.groupid ? `${route.query.groupid}` : (localStorage.getItem('groupid') || `snltty`),
            showPort: false
        });
        localStorage.setItem('api', state.api);
        localStorage.setItem('apipsd', state.apipsd);
        localStorage.setItem('groupid', state.groupid);
        globalData.value.groupid = state.groupid;
        const showPort = computed(() => globalData.value.connected == false && state.showPort);

        const handleConnect = () => {
            initWebsocket(`ws://${state.api}`,state.apipsd);
            localStorage.setItem('api', state.api);
            localStorage.setItem('apipsd', state.apipsd);
            localStorage.setItem('groupid', state.groupid);
            globalData.value.groupid = state.groupid;
        }

        const _getConfig = ()=>{
            getConfig().then((res)=>{
                globalData.value.config.Common = res.Data.Common;
                globalData.value.config.Client = res.Data.Client;
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
            _getConfig();
            _getSignInfoInfo();
            handleConnect();
            setTimeout(() => { state.showPort = true; }, 100);
            subWebsocketState((state) => { if (state) globalData.value.updateFlag = Date.now(); });
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
        li{box-sizing:border-box;padding:.5rem 0;margin-right:.5rem}
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