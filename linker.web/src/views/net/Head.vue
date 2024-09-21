<template>
    <div class="head-wrap">
        <div class="tools flex">
            <span class="label">服务器 </span><el-select v-model="state.server" placeholder="服务器" style="width:16rem" size="small">
                <el-option v-for="item in state.servers":key="item.Host" :label="item.Name":value="item.Host" ></el-option>
            </el-select>
            <span class="flex-1"></span>
            <el-button size="small" @click="handleRefresh">
                刷新(F5)<el-icon><Refresh /></el-icon>
            </el-button>
            <div style="margin-left:1rem ;">
                <Background name="net" ></Background>
            </div>
        </div>
    </div>
</template>

<script>
import { injectGlobalData } from '@/provide';
import { reactive, watch } from 'vue';
import { Edit,Refresh } from '@element-plus/icons-vue';
import Background from '../full/Background.vue';
export default {
    components:{Edit,Refresh,Background},
    setup () {
        const globalData = injectGlobalData();
        const state = reactive({
            server:"linker.snltty.com:1802",
            servers:[]
        });
        watch(()=>globalData.value.config.Client.Servers,()=>{
            state.servers = (globalData.value.config.Client.Servers || []).slice(0,1);
            state.server = globalData.value.config.Client.ServerInfo.Host;
        });
        const handleRefresh = ()=>{
            window.location.reload();
        }

        return {
            state,handleRefresh
        }
    }
}
</script>

<style lang="stylus">
body.sunny{
    background-image:url(../../../public/bg.jpg);
    background-repeat:no-repeat;
    background-size:cover;  
    background-position:center bottom;

    position:absolute;
    left:0;
    top:0;
    right:0;
    bottom:0;
}
body.sunny .status-wrap{
    background-color:rgba(255,255,255,0.5);
}
body.sunny .head-wrap{
    background-color:rgba(255,255,255,0.7);
}
body.sunny .net-wrap{
    background-color:rgba(250,250,250,0.5);
}
body.sunny .net-list-wrap ul li{
    background-color:rgba(250,250,250,0.5);
}

</style>
<style lang="stylus" scoped>
.head-wrap{
    background-color:#fafafa;
    padding:1rem;
    border-bottom:1px solid #ddd;
    box-shadow:1px 2px 3px rgba(0,0,0,.05);

    font-size:1.4rem;

    span.label{
        line-height:2.4rem
        margin-right:.6rem
        color:#555;
    }
}
</style>