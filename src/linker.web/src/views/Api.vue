<template>
    <el-dialog class="options-center" title="管理接口" destroy-on-close v-model="showPort" center :show-close="false" :close-on-click-modal="false" align-center width="200">
        <div class="port-wrap t-c">
            <div>
                接口 : <el-input v-trim v-model="state.api" style="width:70%" @keyup.enter="handleConnectReload"></el-input>
            </div>
            <div class="pdt-10">
                秘钥 : <el-input v-trim show-password type="password" v-model="state.psd" style="width:70%" @keyup.enter="handleConnectReload"></el-input>
            </div>
            <div>
                <el-checkbox v-model="state.save" >保存密码</el-checkbox>
            </div>
        </div>
        <template #footer>
            <el-button type="success" @click="handleConnectReload" plain>确 定</el-button>
        </template>
    </el-dialog>
</template>
<script>
import {useRoute,useRouter} from 'vue-router'
import {injectGlobalData} from '@/provide'
import { computed, nextTick, onMounted, reactive } from 'vue';
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
        
        const api = process.env.NODE_ENV == 'development' ? `${window.location.hostname}:1804` : window.location.host;
        const defaultInfo = {api:api,psd:'snltty'};
        const queryCache = JSON.parse(sessionStorage.getItem('api-cache') || localStorage.getItem('api-cache') || JSON.stringify(defaultInfo));
        const state = reactive({
            api:queryCache.api,
            psd:queryCache.psd,
            showPort: false,
            save: queryCache.save || false,

            timer:0,
            hashcode:0,
            hashcode1:0
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
        const handleConnectReload = ()=>{
            handleConnect();
            window.location.reload();
        }

        const _getConfig = ()=>{
            return new Promise((resolve, reject) => {
                getConfig({hashcode:state.hashcode,hashcode1:state.hashcode1}).then((res)=>{
                    if(res.List.Common)
                        globalData.value.config.Common = res.List.Common;
                    if( res.List.Client)
                        globalData.value.config.Client = res.List.Client;
                    if( res.List.Server)
                        globalData.value.config.Server = res.List.Server;
                    if(res.List.Running)
                        globalData.value.config.Running = res.List.Running;
                    
                    globalData.value.config.configed = true;
                    state.hashcode = res.HashCode;
                    state.hashcode1 = res.HashCode1;

                    document.title = `${globalData.value.config.Client.Name} - linker.web`;

                    if(res.List.Running)window.dispatchEvent(new Event('resize'));

                    resolve();
                }).catch((err)=>{
                    resolve();
                }); 
            });
            
        }
        const _getSignInfoInfo = ()=>{
            return new Promise((resolve, reject) => {
                getSignInfo().then((res)=>{
                    globalData.value.signin.Connected = res.Connected;
                    globalData.value.signin.Version = res.Version;
                    globalData.value.signin.Super = res.Super;
                    globalData.value.signin.SignInHost = res.SignInHost;
                    resolve();
                }).catch((err)=>{
                    resolve();
                });
            });
        }

        const task = ()=>{
            clearTimeout(state.timer);
            Promise.all([_getConfig(),_getSignInfoInfo()]).then(()=>{
                state.timer = setTimeout(task,1000);
            }).catch(()=>{
                state.timer = setTimeout(task,1000);
            });
        }

        onMounted(() => {
            setTimeout(() => { state.showPort = true; }, 500);
            subWebsocketState((connected) => { 
                if (connected)task();
            });
            router.isReady().then(()=>{
                state.api = route.query.api ? `${window.location.hostname}:${route.query.api}`  : state.api;
                state.psd = route.query.psd || state.psd;
                handleConnect();
            });
        });

        return {state,  showPort, handleConnectReload};
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