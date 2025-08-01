<template>
    <div class="head-wrap">
        <div class="tools flex">
            <span class="label">分组 : {{state.group}}</span>
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
import { computed, reactive, watch } from 'vue';
import { Edit,Refresh } from '@element-plus/icons-vue';
import Background from '../full/head/Background.vue';
export default {
    components:{Edit,Refresh,Background},
    setup () {
        const globalData = injectGlobalData();
        const state = reactive({
            server:computed(()=>globalData.value.config.Client.Server.Host),
            group:computed(()=>globalData.value.config.Client.Group.Name),
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
    background-image:url(../../../../public/bg.jpg);
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