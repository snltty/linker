<template>
    <el-dialog class="options-center" title="管理接口" destroy-on-close v-model="showPort" center :show-close="false"
            :close-on-click-modal="false" align-center width="200">
        <div class="port-wrap t-c">
            <div>
                接口 : <el-input v-model="state.api" style="width:70%" @keyup.enter="handleConnect1"></el-input>
            </div>
            <div class="pdt-10">
                秘钥 : <el-input show-password type="password" v-model="state.psd" style="width:70%" @keyup.enter="handleConnect1"></el-input>
            </div>
            <div class="pdt-10">
                <el-checkbox v-model="state.save" >保存密码</el-checkbox>
            </div>
        </div>
        <template #footer>
            <el-button type="success" @click="handleConnect1" plain>确 定</el-button>
        </template>
    </el-dialog>
</template>
<script>
import {useRoute,useRouter} from 'vue-router'
import {injectGlobalData} from '@/provide'
import { computed, onMounted, reactive } from 'vue';
import { initWebsocket, subWebsocketState,closeWebsocket } from '@/apis/request'
import { getSignInfo } from '@/apis/signin'
import { getConfig } from '@/apis/config'
import {Tools} from '@element-plus/icons-vue'
export default {
    components:{Tools},
    props:['config'],
    setup(props) {
        const globalData = injectGlobalData();
        const router = useRouter();
        const route = useRoute();

        const defaultInfo = {api:`${window.location.hostname}:1803`,psd:'snltty'};
        const queryCache = JSON.parse(sessionStorage.getItem('api-cache') || localStorage.getItem('api-cache') || JSON.stringify(defaultInfo));
        const state = reactive({
            api:queryCache.api,
            psd:queryCache.psd,
            showPort: false,
            save: queryCache.save || false,
        });
        const showPort = computed(() => globalData.value.api.connected == false && state.showPort);

        const handleConnect = () => {
            queryCache.api = state.api;
            queryCache.psd = state.psd;
            queryCache.save = state.save;
            if(state.save){
                localStorage.setItem('api-cache',JSON.stringify(queryCache));
            }else{
                localStorage.setItem('api-cache', '');
            }
            sessionStorage.setItem('api-cache',JSON.stringify(queryCache));
            closeWebsocket();
            const url = `ws${window.location.protocol === "https:" ? "s" : ""}://${state.api}`
            initWebsocket(url,state.psd);
        }
        const handleConnect1 = ()=>{
            handleConnect();
            window.location.reload();
        }

        const _getConfig = ()=>{
            getConfig().then((res)=>{
                globalData.value.config.Common = res.Common;
                globalData.value.config.Client = res.Client;
                globalData.value.config.Server = res.Server;
                globalData.value.config.Running = res.Running;
                globalData.value.config.configed = true;
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
                globalData.value.signin.Version = res.Version;
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
            setTimeout(() => { state.showPort = true; }, 500);
            subWebsocketState((state) => { if (state) {
                _getConfig();
                _getSignInfoInfo();
            }});
            router.isReady().then(()=>{
                state.api = route.query.api ? `${window.location.hostname}:${route.query.api}`  : state.api;
                state.psd = route.query.psd || state.psd;
                handleConnect();
            });
        });

        return {state,  showPort, handleConnect1};
    }
}
</script>
<style lang="stylus" scoped>
.status-api-wrap{
    padding-right:2rem;
    a{color:#333;}
    span{border-radius:1rem;background-color:rgba(0,0,0,0.1);padding:0 .6rem;margin-left:.2rem}

    &.connected {
       a{color:green;font-weight:bold;}
       span{background-color:green;color:#fff;}
    }  
    .el-icon{
        vertical-align:text-top;
    }
}

</style>